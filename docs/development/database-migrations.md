# Database migrations

Configure a SQL Server connection as described in [local setup](setup.md#local-development).

Apply the database migrations before using the new inventory fields:

```powershell
dotnet ef database update --project WebApplication
```

`AlignOperationsSchema` first brings the original migration schema in line with
the existing backend model. Existing orders are assigned to an unavailable
"Legacy orders" table and initialized as Draft; review those historical orders
after upgrading. `AddInventoryRestocking` then adds target stock and purchase-price
history. Existing items default to target stock 0 and an empty history.
