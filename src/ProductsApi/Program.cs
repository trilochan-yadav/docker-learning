using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProductsApi.BLL;
using ProductsApi.BLL.Interfaces;
using ProductsApi.DAL;
using ProductsApi.DAL.Interfaces;
using ProductsApi.Data;
using ProductsApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging — Serilog reads its configuration from the "Serilog" section in appsettings.json.
// It replaces the default ASP.NET Core logging provider entirely.
// WriteTo: Console (per-container stdout) + Seq (centralised, multi-instance safe).
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Database ─────────────────────────────────────────────────────────────────
// Connection string is loaded from appsettings.json and can be overridden by the
// environment variable: ConnectionStrings__DefaultConnection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ── Application layers (DI registration) ─────────────────────────────────────
// DAL: EF Core data access
builder.Services.AddScoped<IProductDataService, ProductDataService>();
// BLL: business logic, delegates to DAL
builder.Services.AddScoped<IProductService, ProductService>();

// ── MVC controllers ───────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Products API",
        Version = "v1",
        Description = "A .NET 8 REST API for managing products. All endpoints require the X-Api-Key header."
    });

    // 1. Declare the ApiKey security scheme
    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Description = "Enter your API key in the field below.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme",
        Reference = new OpenApiReference
        {
            Id = "ApiKey",
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition("ApiKey", apiKeyScheme);

    // 2. Apply the scheme globally — every operation shows the padlock icon.
    //    After the user clicks Authorize and enters the key once,
    //    Swagger UI automatically injects X-Api-Key into every Try-it-out request.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { apiKeyScheme, Array.Empty<string>() }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────

// ── Swagger
app.UseSwagger();
//   RoutePrefix     → "swagger"                 (UI accessible at /swagger)
// Explicit options are only needed for multiple API versions, custom UI title,
// serving the UI at the root path (RoutePrefix = ""), injecting custom CSS/JS, etc.
app.UseSwaggerUI();

// Serilog request logging.
app.UseSerilogRequestLogging();

// GlobalExceptionMiddleware converts unhandled exceptions to correct HTTP status
app.UseMiddleware<GlobalExceptionMiddleware>();

// ApiKeyMiddleware validates X-Api-Key on all requests that reach this point.
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

// ── Database initialisation ───────────────────────────────────────────────────
// EnsureCreated creates the schema on first run if it does not already exist.
// For production migrations, replace with db.Database.Migrate() and include
// EF migration files generated via: dotnet ef migrations add InitialCreate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
