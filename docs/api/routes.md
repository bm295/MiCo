# Main API routes

- `GET /api/health`
- `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders`
- `POST /api/orders/{orderId}/items`
- `DELETE /api/orders/{orderId}/items/{orderItemId}`
- `POST /api/orders/{orderId}/send-to-kitchen`
- `POST /api/orders/{orderId}/close`
- `GET /api/inventory`, `POST /api/inventory`
- `PUT /api/inventory/{id}/restocking-settings`
- `POST /api/inventory/restocking-recommendations`
- `GET /api/tables`, `POST /api/tables`
- `GET /api/payments`, `POST /api/payments/orders/{orderId}`
- `GET /api/reports/operations-summary`

See [inventory restocking recommendations](../business/inventory-restock-recommendations.md) for request examples and allocation rules.
