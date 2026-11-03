param([string]$ReportServer = 'http://127.0.0.1/ReportServer')
$ErrorActionPreference = 'Stop'
$uri = [Uri]$ReportServer
if ($uri.Scheme -ne 'https' -and -not ($uri.Scheme -eq 'http' -and $uri.IsLoopback)) { throw 'Use HTTPS or the established local SSRS endpoint.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$definition = Join-Path $root 'src/SyncBridge.Core.Infrastructure/Reporting/Reports/ElectronicBatchRecord.rdl'
$namespace = 'http://schemas.microsoft.com/sqlserver/reporting/2010/03/01/ReportServer'
$handler = [Net.Http.HttpClientHandler]::new(); $handler.UseDefaultCredentials = $true; $handler.AllowAutoRedirect = $false; $handler.UseProxy = $false
$client = [Net.Http.HttpClient]::new($handler)
function Invoke-ReportManagement([string]$Action, [string]$InnerXml) {
    $envelope = "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body><$Action xmlns='$namespace'>$InnerXml</$Action></s:Body></s:Envelope>"
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post, "$ReportServer/ReportService2010.asmx")
    $request.Headers.Add('SOAPAction', "$namespace/$Action")
    $request.Content = [Net.Http.StringContent]::new($envelope, [Text.Encoding]::UTF8, 'text/xml')
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            [xml]$xml = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $fault = $xml.SelectSingleNode("//*[local-name()='faultstring']")
            if ($fault) { throw $fault.InnerText }
            $response.EnsureSuccessStatusCode() | Out-Null
            return $xml
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}
try {
    $data = Invoke-ReportManagement 'GetDataSourceContents' '<DataSource>/SyncBridge/SyncBridgeCoreReadOnly</DataSource>'
    function Field($Name) { $data.SelectSingleNode("//*[local-name()='$Name']").InnerText }
    if ((Field 'CredentialRetrieval') -ne 'Store' -or (Field 'WindowsCredentials') -ne 'true' -or
        (Field 'UserName') -ne 'SB-DEV-01\SyncBridgeReport' -or (Field 'ImpersonateUser') -ne 'false') {
        throw 'The existing shared source must use the approved stored Windows reporting identity.'
    }
    $test = Invoke-ReportManagement 'TestConnectForItemDataSource' '<ItemPath>/SyncBridge/SyncBridgeCoreReadOnly</ItemPath><DataSourceName>SyncBridgeCoreReadOnly</DataSourceName><UserName></UserName><Password></Password>'
    if ($test.SelectSingleNode("//*[local-name()='TestConnectForItemDataSourceResult']").InnerText -ne 'true') { throw 'Restricted data source connection failed.' }
    $encoded = [Convert]::ToBase64String([IO.File]::ReadAllBytes($definition))
    $created = Invoke-ReportManagement 'CreateCatalogItem' "<ItemType>Report</ItemType><Name>ElectronicBatchRecord</Name><Parent>/SyncBridge</Parent><Overwrite>true</Overwrite><Definition>$encoded</Definition><Properties/>"
    # This new report is reachable only by the trusted local application/deployment Windows identity.
    # No end-user Browser group or reporting-data-source account is granted portal access.
    $caller = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    $escaped = [Security.SecurityElement]::Escape($caller)
    Invoke-ReportManagement 'SetPolicies' "<ItemPath>/SyncBridge/ElectronicBatchRecord</ItemPath><Policies><Policy><GroupUserName>$escaped</GroupUserName><Roles><Role><Name>Content Manager</Name></Role></Roles></Policy></Policies>" | Out-Null
    [pscustomobject]@{ ReportPath='/SyncBridge/ElectronicBatchRecord'; DataSource='/SyncBridge/SyncBridgeCoreReadOnly'; DataSourceUser='SB-DEV-01\SyncBridgeReport'; ConnectionTest=$true; DefinitionSHA256=(Get-FileHash -LiteralPath $definition -Algorithm SHA256).Hash; Warnings=@($created.SelectNodes("//*[local-name()='Warning']") | ForEach-Object { $_.InnerText }); ReportAccessWindowsIdentity=$caller } | ConvertTo-Json -Depth 4
} finally { $client.Dispose(); $handler.Dispose() }
