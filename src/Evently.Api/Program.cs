using Evently.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// The runtime image has no Kerberos libraries, so skip the GSS encryption probe instead of failing to load them.
var connection = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Default")) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
builder.Services.AddDbContext<AppDb>(o => o.UseNpgsql(connection));
// Strict numbers keep the OpenAPI schema honest: integers are numbers, not "number or numeric string".
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);
builder.Services.AddOpenApi(o => o.AddSchemaTransformer((schema, _, _) => { RequiredProperties.Apply(schema); return Task.CompletedTask; }));
builder.Services.AddSingleton<FaviconFetcher>();
builder.Services.AddHttpClient<PlacesClient>(c => c.Timeout = TimeSpan.FromSeconds(8));
if (builder.Configuration["DataProtection:Keys"] is { Length: > 0 } keys)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys));
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Editor", p => p.RequireRole(AppRoles.Admin, AppRoles.Editor))
    .AddPolicy("Admin", p => p.RequireRole(AppRoles.Admin));
// Check the security stamp on every request so a role change or password reset applies immediately.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.AddIdentityApiEndpoints<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDb>();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

var app = builder.Build();

// Pending migrations run before the server listens. Each one is transactional; if one fails the exception
// aborts startup, so the app never serves requests against a half-migrated schema.
// The only exception is the build-time OpenAPI generator, which runs the app without a database.
if (System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints();
app.MapUserEndpoints();
app.MapLookupEndpoints();
app.MapCategoryEndpoints();
app.MapTopicEndpoints();
app.MapEventEndpoints();
app.MapCommentEndpoints();
app.MapPlaceEndpoints();
app.MapSearchEndpoints();
app.MapGet("/api/health", () => Results.Ok());

app.MapFallbackToFile("{*path:regex(^(?!api)[^.]*$)}", "index.html");

app.Run();

public partial class Program;
