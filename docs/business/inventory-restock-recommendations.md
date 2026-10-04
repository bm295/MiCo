# Inventory restocking recommendations

Apply the [database migrations](../development/database-migrations.md) before using the new inventory fields.

Configure an existing item's restocking data with
`PUT /api/inventory/1/restocking-settings`:

```json
{
  "targetStock": 7,
  "purchasePriceHistory": [30000, 30000, 30000, 30000, 20000]
}
```

Prices are in VND and ordered oldest to newest. The final entry is the current
purchase price; only the latest five entries participate in the calculation.
These prices are independent of the inventory item's selling `unitPrice`.
The same fields can be supplied when creating an inventory item.

Request a proposal with `POST /api/inventory/restocking-recommendations`:

```json
{ "budget": 100000 }
```

The response includes `purchases` (item ID, name, quantity, purchase price,
relative discount and total cost), `remainingBudget`, and `warnings` for items
with insufficient price history. Eligible items must be below target stock,
have at least five observations, have a positive current price, and have a
positive relative discount: `average(latest five prices) / current price - 1`.
Candidates are ordered by discount descending, then item ID ascending. Each
quantity is capped at the stock shortage and whole units affordable with the
remaining budget. Unaffordable candidates are skipped. Calculations use decimal
arithmetic. A negative budget returns HTTP 400.

Generating a proposal does not change stock, create payments, or execute
purchases. This API implements the scenarios in
[inventory-restock-recommendations.feature](inventory-restock-recommendations.feature); those scenarios are
covered by automated C# tests rather than a Gherkin runner.
