param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
# Read-only evidence only. This script has no test-database provisioning/cleanup commands.
$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=localhost;Database=SyncBridgeCore;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadOnly'
function Read-Table([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $reader = $command.ExecuteReader()
    $table = New-Object System.Data.DataTable
    $table.Load($reader)
    $command.Dispose()
    return ,$table
}
function Read-Json([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $reader = $command.ExecuteReader()
    $builder = New-Object System.Text.StringBuilder
    while ($reader.Read()) { [void]$builder.Append($reader.GetString(0)) }
    $reader.Dispose()
    $command.Dispose()
    return $builder.ToString()
}
try {
    $connection.Open()
    $identity = Read-Table 'SELECT DB_NAME() AS DatabaseName'
    if ($identity.Rows[0].DatabaseName -cne 'SyncBridgeCore') { throw 'Unexpected preservation database identity.' }
    $tables = Read-Table @'
SELECT t.name, STRING_AGG(CONVERT(nvarchar(max), QUOTENAME(c.name)), ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
FROM sys.tables t
JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1
JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
WHERE t.schema_id=SCHEMA_ID('dbo') AND t.name <> '__EFMigrationsHistory'
GROUP BY t.name ORDER BY t.name
'@
    if ($tables.Rows.Count -ne 15) { throw 'Expected exactly 15 manufacturing tables.' }
    $hashes = foreach ($row in $tables.Rows) {
        $quotedName = '[' + $row.name.Replace(']', ']]') + ']'
        $json = Read-Json ("SELECT * FROM dbo.$quotedName ORDER BY " + $row.KeyColumns + ' FOR JSON PATH, INCLUDE_NULL_VALUES')
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))).Replace('-','') }
        finally { $sha.Dispose() }
        $count = Read-Table "SELECT COUNT(*) AS [RowCount] FROM dbo.$quotedName"
        [ordered]@{Table=$row.name; Rows=[int]$count.Rows[0].RowCount; OrderedRowSHA256=$hash}
    }
    $demo = Read-Json @'
SELECT b.BatchNumber, b.Status, p.OrderNumber, p.Status AS OrderStatus,
 (SELECT COUNT(*) FROM dbo.BatchStepExecution x WHERE x.BatchId=b.BatchId) AS Steps,
 (SELECT COUNT(*) FROM dbo.MaterialVerification x WHERE x.BatchId=b.BatchId) AS MaterialVerifications,
 (SELECT COUNT(*) FROM dbo.EquipmentVerification x WHERE x.BatchId=b.BatchId) AS EquipmentVerifications,
 (SELECT COUNT(*) FROM dbo.Deviation x WHERE x.BatchId=b.BatchId) AS Deviations,
 (SELECT COUNT(*) FROM dbo.ElectronicSignature x WHERE x.BatchId=b.BatchId) AS Signatures,
 (SELECT COUNT(*) FROM dbo.AuditEvent x WHERE x.BatchId=b.BatchId) AS BatchAudits,
 (SELECT COUNT(*) FROM dbo.AuditEvent x WHERE x.BatchId IS NULL AND x.AffectedEntity='ProductionOrder' AND x.AffectedEntityId=p.OrderNumber) AS OrderAudits
FROM dbo.Batch b JOIN dbo.ProductionOrder p ON p.OrderId=b.OrderId
WHERE b.BatchNumber=N'A3-B-20261014' AND p.OrderNumber=N'A3-PO-20261014'
FOR JSON PATH, INCLUDE_NULL_VALUES
'@
    $records = @($demo | ConvertFrom-Json)
    if ($records.Count -ne 1 -or $records[0].Status -ne 'Completed' -or $records[0].OrderStatus -ne 'Completed') { throw 'Retained A3 Completed records were not found.' }
    $migration = Read-Json 'SELECT * FROM dbo.__EFMigrationsHistory ORDER BY MigrationId FOR JSON PATH, INCLUDE_NULL_VALUES'
    [ordered]@{Database='SyncBridgeCore'; ManufacturingTables=@($hashes); A3=$records; Migrations=($migration | ConvertFrom-Json)} |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath
}
finally { $connection.Dispose() }
