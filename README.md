# MilkCO FnB Management

MilkCO provides inventory tracking, order capture with stock validation and deduction, payment tracking, and PMP project documents under `docs/`.

## Stack and architecture

- Angular 21 standalone client in `ClientApp/` (Node.js 24, npm)
- C# 14 / ASP.NET Core 10 REST API in `WebApplication/`
- Entity Framework Core 10 and SQL Server
- Docker Compose

The Angular client uses relative `/api` URLs. During development Angular proxies these to .NET; in production .NET serves the built client from `wwwroot` and handles SPA routes. Backend use cases remain in `Application/Services`, repository contracts in `Application/Ports`, and EF adapters in `Infrastructure/Persistence`.

## Run with Docker

```bash
docker compose up --build -d
```

Open `http://localhost:8080/orders`. The multi-stage Dockerfile builds Angular and .NET, then packages both in the ASP.NET runtime image.

The API connects to SQL Server through the `sqlserver` Compose service. Configure `ConnectionStrings__DefaultConnection` in `docker-compose.yml` for different database credentials. Stop with `docker compose down`.

## Local development

Install .NET SDK 10 and Node.js 24. Start SQL Server, for example:

```bash
docker compose up -d sqlserver
```

Configure the backend connection without editing committed settings (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost,14333;Database=MilkCoPOSDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;MultipleActiveResultSets=true'
dotnet run --project WebApplication/WebApplication.csproj --urls http://localhost:5000
```

In a second terminal:

```bash
cd ClientApp
npm ci
npm start
```

Open `http://localhost:4200/orders`. `ClientApp/proxy.conf.json` forwards `/api/**` to `http://localhost:5000`; update the target if the backend uses another port. The database must already have the EF schema applied; apply migrations with `dotnet ef database update --project WebApplication` if needed.

## Build and serve through .NET

```bash
cd ClientApp
npm ci
npm run build:hosted
cd ..
dotnet run --project WebApplication/WebApplication.csproj
```

Open `http://localhost:5000/orders`. `build:hosted` builds the client and copies browser assets to `WebApplication/wwwroot`. Run it again after changing the client and before publishing .NET locally. Generated assets are excluded from Git.

## Verification

```bash
dotnet test HelloWorldMvc.sln
cd ClientApp
npm run build
npm test
```

## Order flow

1. Add inventory and available tables through the APIs if the catalog is empty.
2. Open `/orders`, enter a customer name, select a table, and choose quantities.
3. Submit the order; the backend validates stock and saves the order.
4. Review `/orders/Confirmation/{id}`. This route also supports direct navigation and reloads.

The client displays loading, empty-stock, unavailable-table, and API-error states. Failed submissions refresh the stock snapshot while preserving the entered customer and quantities.

## Main API routes

- `GET /api/health`
- `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders`
- `POST /api/orders/{orderId}/items`
- `DELETE /api/orders/{orderId}/items/{orderItemId}`
- `POST /api/orders/{orderId}/send-to-kitchen`
- `POST /api/orders/{orderId}/close`
- `GET /api/inventory`, `POST /api/inventory`
- `GET /api/tables`, `POST /api/tables`
- `GET /api/payments`, `POST /api/payments/orders/{orderId}`
- `GET /api/reports/operations-summary`

## Project documents

- Charter: `docs/01-project-charter/`
- PMP process library: `docs/pmp-processes/`
