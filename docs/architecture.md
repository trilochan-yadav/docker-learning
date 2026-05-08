# Architecture — Products API

## 1. Application Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         HTTP Client                                  │
│              (curl / Swagger UI / Postman / Frontend)               │
└───────────────────────────────┬─────────────────────────────────────┘
                                │  HTTP  X-Api-Key header
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        ASP.NET Core Pipeline                         │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  GlobalExceptionMiddleware  (outermost)                      │   │
│  │  Catches all unhandled exceptions → JSON error response      │   │
│  │  KeyNotFoundException → 404 | ArgumentException → 400       │   │
│  │  InvalidOperationException → 400 | Other → 500              │   │
│  │                                                              │   │
│  │  ┌────────────────────────────────────────────────────────┐  │   │
│  │  │  ApiKeyMiddleware                                       │  │   │
│  │  │  Validates X-Api-Key header against ApiSettings:ApiKey │  │   │
│  │  │  /swagger/* paths are exempt → UI loads freely        │  │   │
│  │  │  All /api/* paths require the key → 401 if invalid    │  │   │
│  │  │                                                         │  │   │
│  │  │  ┌─────────────────────────────────────────────────┐   │  │   │
│  │  │  │  ProductsController  [ApiController]            │   │  │   │
│  │  │  │  GET /api/products                              │   │  │   │
│  │  │  │  GET /api/products/{id}                         │   │  │   │
│  │  │  │  POST /api/products                             │   │  │   │
│  │  │  │  PUT  /api/products/{id}                        │   │  │   │
│  │  │  │  DELETE /api/products/{id}                      │   │  │   │
│  │  │  │  PATCH /api/products/{id}/reduce-stock          │   │  │   │
│  │  │  │  PATCH /api/products/{id}/restore-stock         │   │  │   │
│  │  │  └──────────────────┬──────────────────────────────┘   │  │   │
│  │  │                     │ calls IProductService             │  │   │
│  │  │  ┌──────────────────▼──────────────────────────────┐   │  │   │
│  │  │  │  BLL — ProductService  (IProductService)        │   │  │   │
│  │  │  │  • GetAll / GetById / Create / Update / Delete  │   │  │   │
│  │  │  │  • ReduceStock  → validates qty > 0, stock ≥ qty│   │  │   │
│  │  │  │  • RestoreStock → validates qty > 0             │   │  │   │
│  │  │  │  Throws typed exceptions consumed by middleware  │   │  │   │
│  │  │  └──────────────────┬──────────────────────────────┘   │  │   │
│  │  │                     │ calls IProductDataService         │  │   │
│  │  │  ┌──────────────────▼──────────────────────────────┐   │  │   │
│  │  │  │  DAL — ProductDataService  (IProductDataService)│   │  │   │
│  │  │  │  • EF Core CRUD operations                      │   │  │   │
│  │  │  │  • AdjustStock(id, delta) applies +/- directly  │   │  │   │
│  │  │  │  No business rules — pure data persistence      │   │  │   │
│  │  │  └──────────────────┬──────────────────────────────┘   │  │   │
│  │  │                     │ EF Core queries                   │  │   │
│  │  │  ┌──────────────────▼──────────────────────────────┐   │  │   │
│  │  │  │  AppDbContext  (EF Core DbContext)               │   │  │   │
│  │  │  │  DbSet<Product>                                  │   │  │   │
│  │  │  └──────────────────┬──────────────────────────────┘   │  │   │
│  │  └─────────────────────┼─────────────────────────────────-┘  │   │
│  └────────────────────────┼──────────────────────────────────────┘   │
└───────────────────────────┼─────────────────────────────────────────┘
                            │  SQL over TCP 1433
                            ▼
                  ┌─────────────────────┐
                  │  SQL Server Express  │
                  │  Database: ProductsDb│
                  │  Table: Products     │
                  └─────────────────────┘
```

---

## 2. Container / Docker Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Docker Host  (desktop / Codespace VM)               │
│                                                                          │
│  Host port 5000 ──────────────────────┐                                 │
│  Host port 1433 ────────────┐         │                                 │
│                             │         │                                  │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │               Docker Network: app-network  (bridge)             │    │
│  │                                                                  │    │
│  │  ┌──────────────────────────────┐  ┌──────────────────────────┐ │    │
│  │  │  Container: sqlserver        │  │  Container: products-api  │ │    │
│  │  │  Image: mssql/server:2022    │  │  Image: products-api:*    │ │    │
│  │  │                              │  │  (.NET 8 / aspnet:8.0)   │ │    │
│  │  │  Port: 1433 ◄───────────────────  connects → sqlserver:1433 │ │    │
│  │  │  Volume: sqldata             │  │                           │ │    │
│  │  │  (/var/opt/mssql)            │  │  Port: 8080               │ │    │
│  │  │                              │  │  User: appuser (non-root) │ │    │
│  │  │  Healthcheck: sqlcmd SELECT 1│  │                           │ │    │
│  │  │  (API waits for healthy)     │  │  Layers:                  │ │    │
│  │  └──────────────────────────────┘  │  GlobalExceptionMiddleware│ │    │
│  │                 ▲                  │  ApiKeyMiddleware         │ │    │
│  │                 │                  │  ProductsController       │ │    │
│  │          sqldata volume            │  BLL / DAL / EF Core      │ │    │
│  │          (named, local driver)     └──────────────────────────-┘ │    │
│  └──────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Multi-Stage Dockerfile Stages

| Stage | Base Image | Purpose |
|-------|-----------|---------|
| `build` | `mcr.microsoft.com/dotnet/sdk:8.0` | Restore NuGet packages, compile, publish Release build |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Lightweight runtime image; only published output is copied across |

Benefits:
- Final image contains **no SDK, no source code, no NuGet cache** — smallest possible attack surface.
- Build layer caching: the `dotnet restore` step is re-used until `ProductsApi.csproj` changes.
- Non-root user (`appuser`) limits privilege in the running container.

---

## 4. Data Flow — Example: PATCH /api/products/1/reduce-stock

```
1. Client sends:
   PATCH /api/products/1/reduce-stock
   X-Api-Key: my-super-secret-api-key-12345
   Body: { "quantity": 5 }

2. GlobalExceptionMiddleware — begins try/catch wrapper

3. ApiKeyMiddleware — validates X-Api-Key → pass

4. ProductsController.ReduceStock(1, dto)
   → calls IProductService.ReduceStockAsync(1, dto)

5. ProductService.ReduceStockAsync
   a. dto.Quantity > 0? yes → continue
   b. GetByIdAsync(1) → calls DAL → product found (Stock = 20)
   c. product.Stock (20) >= dto.Quantity (5)? yes → continue
   d. AdjustStockAsync(1, -5) → DAL applies delta, saves

6. Controller returns 204 No Content

──── Error path example ────
   If Stock = 3 and quantity = 5:
   ProductService throws InvalidOperationException("Insufficient stock...")
   GlobalExceptionMiddleware catches it → 400 Bad Request
   Response: { "statusCode": 400, "message": "Insufficient stock. Available: 3, requested reduction: 5." }
```

---

## 5. Port Map

| Service | Container Port | Host Port | Protocol |
|---------|---------------|-----------|----------|
| products-api | 8080 | 5000 | HTTP |
| sqlserver | 1433 | 1433 | TDS (SQL) |

Swagger UI: `http://localhost:5000/swagger`

---

## 6. Environment Variable Overrides

.NET configuration binds double-underscore env vars to nested config sections:

| Environment Variable | Config Key | Default |
|---------------------|-----------|---------|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | (see appsettings.json) |
| `ApiSettings__ApiKey` | `ApiSettings:ApiKey` | `my-super-secret-api-key-12345` |
| `MSSQL_SA_PASSWORD` | (SQL Server only) | `YourStrong@Password123` |
