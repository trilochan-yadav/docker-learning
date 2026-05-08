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

// ── Database initialisation & seeding ───────────────────────────────────────
// EnsureCreated creates the schema on first run if it does not already exist.
// Seeding runs only when the Products table is empty so it is safe to call on
// every startup — restarting the container will NOT re-insert duplicate rows.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Create schema if it does not already exist
    db.Database.EnsureCreated();

    // Seed default products only when the table is completely empty
    if (!db.Products.Any())
    {
        logger.LogInformation("Seeding default products into the database...");

        db.Products.AddRange(
            new ProductsApi.Models.Product { Name = "Laptop Pro 15", Price = 1299.99m, Stock = 25 },
            new ProductsApi.Models.Product { Name = "Wireless Mouse", Price = 29.99m, Stock = 150 },
            new ProductsApi.Models.Product { Name = "Mechanical Keyboard", Price = 89.99m, Stock = 75 },
            new ProductsApi.Models.Product { Name = "4K Monitor 27inch", Price = 449.99m, Stock = 30 },
            new ProductsApi.Models.Product { Name = "USB-C Hub 7-in-1", Price = 49.99m, Stock = 200 },
            new ProductsApi.Models.Product { Name = "Webcam 1080p", Price = 69.99m, Stock = 60 },
            new ProductsApi.Models.Product { Name = "Noise-Cancelling Headphones", Price = 199.99m, Stock = 40 },
            new ProductsApi.Models.Product { Name = "External SSD 1TB", Price = 109.99m, Stock = 90 }
        );

        db.SaveChanges();
        logger.LogInformation("Seeded {Count} products successfully.", db.Products.Count());
    }
}

app.Run();
