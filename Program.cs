using ListManager.Data;
using ListManager.Services;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

var supportedCultures = new[] { new CultureInfo("en-US") };
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

// A paused serverless database can take a minute or more to resume.
var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
if (connectionStringBuilder.ConnectTimeout < 120)
    connectionStringBuilder.ConnectTimeout = 120;
connectionString = connectionStringBuilder.ConnectionString;

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(120);
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
    }));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<IListItemService, ListItemService>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Separate JWT bearer scheme used only by the JSON API (see Controllers/Api) so the
// Android client can authenticate with an Entra ID access token without disturbing the
// existing cookie/OpenID Connect sign-in used by the web UI.
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdApi"), jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme);

// Normalizes Entra ID tokens so the stable object ID (oid) becomes ClaimTypes.NameIdentifier
// and an email claim is always present, regardless of which scheme issued the principal.
static void NormalizeEntraIdClaims(ClaimsIdentity identity)
{
    var oid = identity.FindFirst("oid")?.Value
        ?? identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    if (oid != null)
    {
        foreach (var claim in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, oid));
    }

    if (identity.FindFirst(ClaimTypes.Email) == null)
    {
        var email = identity.FindFirst("email")?.Value
            ?? identity.FindFirst("preferred_username")?.Value;
        if (email != null)
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
    }
}

// Use the stable Entra object ID as the user ID and expose the email claim the app expects.
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var onTokenValidated = options.Events.OnTokenValidated;
    options.Events.OnTokenValidated = async context =>
    {
        await onTokenValidated(context);

        if (context.Principal?.Identity is ClaimsIdentity identity)
            NormalizeEntraIdClaims(identity);
    };
});

// Apply the same claim normalization to the API's JWT bearer tokens.
builder.Services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var onTokenValidated = options.Events?.OnTokenValidated;
    options.Events ??= new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents();
    options.Events.OnTokenValidated = async context =>
    {
        if (onTokenValidated != null)
            await onTokenValidated(context);

        if (context.Principal?.Identity is ClaimsIdentity identity)
            NormalizeEntraIdClaims(identity);
    };
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddJsonOptions(options =>
    {
        // The JSON API (Controllers/Api) serializes enums (ListType, ListSharePermission)
        // as strings so the Android client can deserialize them without numeric mapping.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Lists}/{action=Index}/{id?}");
app.MapRazorPages();

try
{
    app.Run();
}
catch (IOException ex) when (ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Startup failed because one of the configured URLs is already in use. Stop the existing process or change the launch profile URL.");
    throw;
}
