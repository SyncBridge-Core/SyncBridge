using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SyncBridge.Core.Application.Contracts.Identity;

var builder = WebApplication.CreateBuilder(args);

// Resolve runtime implementations only at the composition root, without a Presentation compile dependency.
var dependencyResolver = new AssemblyDependencyResolver(Path.Combine(AppContext.BaseDirectory, "SyncBridge.Core.Infrastructure.dll"));
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = dependencyResolver.ResolveAssemblyToPath(name) ?? Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
{
    var path = dependencyResolver.ResolveUnmanagedDllToPath(name);
    return path is null ? IntPtr.Zero : System.Runtime.InteropServices.NativeLibrary.Load(path);
};
var infrastructure = Assembly.Load("SyncBridge.Core.Infrastructure");
infrastructure.GetType("SyncBridge.Core.Infrastructure.ApplicationComposition", throwOnError: true)!
    .GetMethod("Configure", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [builder.Services, builder.Configuration]);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/authentication";
    options.Cookie.Name = "__Host-SyncBridge";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            await Results.Problem(statusCode: 401, title: "Unauthenticated").ExecuteAsync(context.HttpContext);
        else context.Response.Redirect(context.RedirectUri);
    };
    options.Events.OnRedirectToAccessDenied = async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            await Results.Problem(statusCode: 403, title: "Forbidden").ExecuteAsync(context.HttpContext);
        else context.Response.Redirect(context.RedirectUri);
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        var previous = context.HttpContext.User;
        try
        {
            context.HttpContext.User = context.Principal!;
            if (await context.HttpContext.RequestServices.GetRequiredService<IActorContext>().GetActorAsync(context.HttpContext.RequestAborted) is null)
                context.RejectPrincipal();
        }
        finally { context.HttpContext.User = previous; }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(options => options.Filters.Add<SyncBridge.Core.Controllers.ApiAntiforgeryResultFilter>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Keep API faults structured and confidential in every hosting environment.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
    api.UseExceptionHandler(error => error.Run(context =>
        Results.Problem(statusCode: 500, title: "The operation could not be completed.").ExecuteAsync(context))));

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

// Preserve the accepted credential/cookie boundary; Razor Pages supplies the sign-in form.
app.MapPost("/authentication/login", async (HttpContext context, IAntiforgery antiforgery, ICredentialAuthenticator credentials) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var identity = await credentials.AuthenticateAsync(form["userName"].ToString(), form["password"].ToString(), context.RequestAborted);
    if (identity is null) return Results.Text("Sign in failed. Check your credentials and active account access.", statusCode: StatusCodes.Status401Unauthorized);
    var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, identity.UserId), new Claim(ClaimTypes.Name, identity.UserName)], CookieAuthenticationDefaults.AuthenticationScheme));
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });
    var returnUrl = form["returnUrl"].ToString();
    return Results.LocalRedirect(Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult.IsLocalUrl(returnUrl) ? returnUrl : "/");
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/authentication/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/authentication");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
