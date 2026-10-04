# Order flow

1. Add inventory and available tables through the APIs if the catalog is empty.
2. Open `/orders`, enter a customer name, select a table, and choose quantities.
3. Submit the order; the backend validates stock and saves the order.
4. Review `/orders/Confirmation/{id}`. This route also supports direct navigation and reloads.

The client displays loading, empty-stock, unavailable-table, and API-error states. Failed submissions refresh the stock snapshot while preserving the entered customer and quantities.

See the [API routes](../api/routes.md) for inventory, tables, and order operations.
