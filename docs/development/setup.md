# Setup and running MilkCO

Run commands from the repository root unless a command changes directories.

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

See [database migrations](database-migrations.md) for schema upgrades.
