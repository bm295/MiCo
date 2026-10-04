# Stack and architecture

- Angular 21 standalone client in `ClientApp/` (Node.js 24, npm)
- C# 14 / ASP.NET Core 10 REST API in `WebApplication/`
- Entity Framework Core 10 and SQL Server
- Docker Compose

The Angular client uses relative `/api` URLs. During development Angular proxies these to .NET; in production .NET serves the built client from `wwwroot` and handles SPA routes. Backend use cases remain in `Application/Services`, repository contracts in `Application/Ports`, and EF adapters in `Infrastructure/Persistence`.
