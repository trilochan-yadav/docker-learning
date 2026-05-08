# Products API — Docker Assignment

A containerized **.NET 8 REST API** for Products CRUD and stock management, backed by **SQL Server Express 2022**, orchestrated with **Docker Compose**.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Quick Start — Docker Desktop](#quick-start--docker-desktop)
5. [GitHub Codespaces / Docker-in-Docker](#github-codespaces--docker-in-docker-dind)
6. [Environment Variables](#environment-variables)
7. [API Endpoints](#api-endpoints)
8. [Authentication](#authentication)
9. [Docker Command Cheatsheet](#docker-command-cheatsheet)
10. [Docker Hub — Push & Pull](#docker-hub--push--pull)

---

## Project Structure

```
Docker Assignment/
├── src/
│   └── ProductsApi/
│       ├── BLL/
│       │   ├── Interfaces/IProductService.cs   # BLL contract
│       │   └── ProductService.cs               # Business rules
│       ├── Controllers/
│       │   └── ProductsController.cs           # HTTP endpoints
│       ├── DAL/
│       │   ├── Interfaces/IProductDataService.cs  # DAL contract
│       │   └── ProductDataService.cs           # EF Core persistence
│       ├── Data/
│       │   └── AppDbContext.cs                 # EF Core DbContext
│       ├── DTOs/
│       │   ├── CreateProductDto.cs
│       │   ├── UpdateProductDto.cs
│       │   └── StockAdjustDto.cs
│       ├── Middleware/
│       │   ├── ApiKeyMiddleware.cs             # X-Api-Key authentication
│       │   └── GlobalExceptionMiddleware.cs    # Global error handling
│       ├── Models/
│       │   └── Product.cs                      # Entity
│       ├── Program.cs                          # App bootstrap + DI
│       ├── appsettings.json
│       └── ProductsApi.csproj
├── Dockerfile                                  # Multi-stage build
├── .dockerignore
├── .gitignore
├── docker-compose.yml                          # Multi-container orchestration
└── docs/
    └── architecture.md                         # Detailed architecture diagrams
```

---

## Architecture

See [docs/architecture.md](docs/architecture.md) for full diagrams.

**Call chain:**
```
HTTP Request
  → GlobalExceptionMiddleware  (catches all exceptions → JSON error)
    → ApiKeyMiddleware          (validates X-Api-Key header)
      → ProductsController
          → IProductService (BLL — business rules)
              → IProductDataService (DAL — EF Core)
                  → AppDbContext → SQL Server
```

**Containers:**

| Container | Image | Host Port |
|-----------|-------|-----------|
| `products-api` | `.NET 8 / aspnet:8.0` | `5000 → 8080` |
| `sqlserver` | `mssql/server:2022-latest` | `1433 → 1433` |

---

## Prerequisites

| Tool | Minimum Version | Check |
|------|----------------|-------|
| Docker Engine | 20.10 | `docker --version` |
| Docker Compose | v2 (CLI plugin) | `docker compose version` |
| Git | any | `git --version` |

> **Docker Desktop** (Windows/macOS) bundles both Docker Engine and Compose v2.

---

## Quick Start — Docker Desktop

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/docker-basic-assignment.git
cd "docker-basic-assignment"
```

### 2. Build and start all services

```bash
docker compose up --build
```

Docker Compose will:
- Build the `products-api` image from the multi-stage `Dockerfile`
- Pull the `mssql/server:2022-latest` image
- Start SQL Server and wait for its healthcheck to pass
- Start the API (it auto-creates the `ProductsDb` schema on first run)

### 3. Verify services are running

```bash
docker compose ps
```

Expected output:
```
NAME            IMAGE                         STATUS          PORTS
products-api    docker-basic-assignment-...   Up              0.0.0.0:5000->8080/tcp
sqlserver       mssql/server:2022-latest      Up (healthy)    0.0.0.0:1433->1433/tcp
```

### 4. Open Swagger UI

Navigate to **http://localhost:5000/swagger**

Click **Authorize**, enter the API key `my-super-secret-api-key-12345`, and confirm.
All subsequent Try-it-out requests will automatically include the `X-Api-Key` header.

### 5. Stop and clean up

```bash
# Stop containers (keeps the sqldata volume)
docker compose down

# Stop containers AND remove the database volume
docker compose down -v
```

---

## GitHub Codespaces / Docker-in-Docker (DinD)

GitHub Codespaces provisions a Linux VM with **Docker-in-Docker pre-installed**.
Your `docker` commands run against an inner Docker daemon, so the workflow is
identical to Docker Desktop.

### Step-by-step

1. **Open Codespace** — in your GitHub repository click  
   **Code → Codespaces → Create codespace on main**

2. **Wait for the environment to provision** (~1–2 minutes)

3. **In the integrated terminal**, run:

   ```bash
   docker compose up --build -d
   ```

   The `-d` flag runs containers in the background so the terminal stays free.

4. **Forward port 5000** — Codespaces auto-detects the listening port and shows
   a notification. Click **Open in Browser**, or go to the **Ports** tab and click
   the globe icon next to port 5000.

5. **Append `/swagger`** to the forwarded URL to open the Swagger UI.

### Why DinD works in Codespaces

Codespaces uses a `docker:dind` sidecar container that exposes a Docker socket
inside the dev container. The `docker` CLI in your terminal talks to that socket,
so `docker build`, `docker compose`, and `docker push` all work without any
additional configuration.

### Codespaces environment variables

To override the API key or SA password in Codespaces, add them as
**Codespace secrets** in your GitHub repository settings
(*Settings → Secrets and variables → Codespaces*):

| Secret name | Maps to |
|-------------|---------|
| `API_KEY` | `ApiSettings__ApiKey` |
| `MSSQL_SA_PASSWORD` | SQL Server SA password |

Then reference them in the terminal:

```bash
API_KEY=$API_KEY MSSQL_SA_PASSWORD=$MSSQL_SA_PASSWORD docker compose up --build -d
```

---

## Environment Variables

All sensitive values can be overridden without changing `appsettings.json`:

| Variable | Default | Used by |
|----------|---------|---------|
| `MSSQL_SA_PASSWORD` | `YourStrong@Password123` | SQL Server + connection string |
| `API_KEY` | `my-super-secret-api-key-12345` | products-api (`ApiSettings__ApiKey`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | products-api |

Override on the command line:

```bash
MSSQL_SA_PASSWORD=MyNewPass!456 API_KEY=new-key docker compose up --build
```

---

## API Endpoints

All endpoints require the `X-Api-Key` header.

| Method | Endpoint | Body | Response | Description |
|--------|----------|------|----------|-------------|
| `GET` | `/api/products` | — | `200` + `Product[]` | List all products |
| `GET` | `/api/products/{id}` | — | `200` / `404` | Get by ID |
| `POST` | `/api/products` | `CreateProductDto` | `201` + `Product` | Create a product |
| `PUT` | `/api/products/{id}` | `UpdateProductDto` | `204` / `404` | Update a product |
| `DELETE` | `/api/products/{id}` | — | `204` / `404` | Delete a product |
| `PATCH` | `/api/products/{id}/reduce-stock` | `StockAdjustDto` | `204` / `400` / `404` | Reduce stock |
| `PATCH` | `/api/products/{id}/restore-stock` | `StockAdjustDto` | `204` / `400` / `404` | Restore stock |

### Request / response schemas

**CreateProductDto / UpdateProductDto**
```json
{
  "name":  "Laptop",
  "price": 999.99,
  "stock": 50
}
```

**StockAdjustDto**
```json
{ "quantity": 5 }
```

**Product** (response)
```json
{
  "id":        1,
  "name":      "Laptop",
  "price":     999.99,
  "stock":     45,
  "createdAt": "2026-05-07T10:00:00Z"
}
```

**Error envelope** (4xx / 5xx)
```json
{ "statusCode": 404, "message": "Product with id 99 was not found." }
```

### curl examples

```bash
BASE=http://localhost:5000
KEY="my-super-secret-api-key-12345"

# Create a product
curl -s -X POST "$BASE/api/products" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","price":999.99,"stock":50}' | jq

# List all products
curl -s "$BASE/api/products" -H "X-Api-Key: $KEY" | jq

# Get by ID
curl -s "$BASE/api/products/1" -H "X-Api-Key: $KEY" | jq

# Update
curl -s -X PUT "$BASE/api/products/1" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{"name":"Gaming Laptop","price":1299.99,"stock":30}'

# Reduce stock by 5
curl -s -X PATCH "$BASE/api/products/1/reduce-stock" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{"quantity":5}'

# Restore stock by 10
curl -s -X PATCH "$BASE/api/products/1/restore-stock" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{"quantity":10}'

# Delete
curl -s -X DELETE "$BASE/api/products/1" -H "X-Api-Key: $KEY"
```

---

## Authentication

The API uses **API Key authentication** via the `X-Api-Key` HTTP header.

- The key is configured in `appsettings.json` under `ApiSettings:ApiKey`
- Override it at runtime with the `ApiSettings__ApiKey` environment variable
- Swagger UI: click **Authorize** (padlock) → enter the key → all Try-it-out requests send the header automatically
- Missing or wrong key returns `401 Unauthorized`

---

## Docker Command Cheatsheet

```bash
# ── Images ────────────────────────────────────────────────────────────────────
docker images                        # list all local images
docker build -t products-api:latest .  # build image from Dockerfile
docker rmi products-api:latest       # remove an image

# ── Containers ────────────────────────────────────────────────────────────────
docker ps                            # list running containers
docker ps -a                         # list all containers (including stopped)
docker run -d -p 5000:8080 --name products-api products-api:latest
docker start  products-api           # start a stopped container
docker stop   products-api           # stop a running container
docker rm     products-api           # remove a stopped container
docker rm -f  products-api           # force-remove a running container

# ── Logs & Inspection ─────────────────────────────────────────────────────────
docker logs   products-api           # view logs (last N lines)
docker logs   products-api -f        # stream / follow logs
docker inspect products-api          # full JSON metadata
docker inspect products-api | jq '.[0].NetworkSettings'  # network details
docker exec -it products-api sh      # open a shell inside the container
docker stats                         # live CPU / memory usage

# ── Docker Compose ────────────────────────────────────────────────────────────
docker compose up --build            # build and start all services
docker compose up --build -d         # same, in detached (background) mode
docker compose ps                    # list compose-managed containers
docker compose logs -f               # stream logs from all services
docker compose logs products-api -f  # stream logs from one service
docker compose stop                  # stop (keep containers)
docker compose down                  # stop and remove containers
docker compose down -v               # also remove named volumes
docker compose restart products-api  # restart a single service

# ── Volumes ───────────────────────────────────────────────────────────────────
docker volume ls                     # list volumes
docker volume inspect sqldata        # inspect the sql data volume
docker volume rm sqldata             # remove volume (data loss!)

# ── Networks ──────────────────────────────────────────────────────────────────
docker network ls                    # list networks
docker network inspect app-network   # inspect the bridge network
```

---

## Docker Hub — Push & Pull

### Push your image

```bash
# 1. Log in to Docker Hub
docker login

# 2. Tag the local image with your Docker Hub username
docker tag products-api:latest <your-dockerhub-username>/products-api:latest

# 3. Push to Docker Hub
docker push <your-dockerhub-username>/products-api:latest
```

### Pull and run from Docker Hub

```bash
# Pull the image
docker pull <your-dockerhub-username>/products-api:latest

# Run it (requires a SQL Server instance reachable at sqlserver:1433,
# or override the connection string)
docker run -d \
  -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Server=<host>,1433;Database=ProductsDb;User Id=sa;Password=<pwd>;TrustServerCertificate=True;" \
  -e ApiSettings__ApiKey="my-super-secret-api-key-12345" \
  --name products-api \
  <your-dockerhub-username>/products-api:latest
```

### Use the Hub image in docker-compose.yml

Replace the `build:` block in `docker-compose.yml` with:

```yaml
products-api:
  image: <your-dockerhub-username>/products-api:latest
  # remove the build: section
```

Then run:

```bash
docker compose pull
docker compose up
```
