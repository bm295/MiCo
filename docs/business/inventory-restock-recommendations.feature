Feature: Recommend inventory restocking within a purchasing budget
  As an inventory manager
  I want to prioritize discounted items that need restocking
  So that I can allocate the available purchasing budget to replenishing stock

  # Adapted from the stock-trading strategy: calculate a relative discount,
  # rank candidates, and allocate the remaining budget in that order.
  # Purchase prices are separate from the selling UnitPrice.
  # A relative discount is the average of the latest five purchase prices
  # divided by the current purchase price, minus one.
  # The current purchase price is the fifth price in that history.
  # Only items below their target stock with a positive discount are candidates.
  # Equal discounts are ordered by item ID ascending for reproducible results.
  # Recommended quantity is the smaller of the stock shortage and the whole
  # units affordable with the remaining budget. Money uses decimal arithmetic.

  Background:
    Given an inventory manager is preparing a restocking recommendation
    And all purchase prices and the purchasing budget are expressed in VND

  Scenario: Allocate budget to the largest relative discount first
    Given the purchasing budget is 100000
    And the inventory and latest five purchase prices are:
      | item ID | item name | stock | target stock | price 1 | price 2 | price 3 | price 4 | price 5 |
      | 1       | Milk      | 2     | 7            | 30000   | 30000   | 30000   | 30000   | 20000   |
      | 2       | Tea       | 1     | 6            | 12000   | 12000   | 12000   | 12000   | 10000   |
    When the manager requests a restocking recommendation
    Then Milk has a relative discount of 0.40
    And Tea has a relative discount of 0.16
    And the recommendation contains these purchases in order:
      | item name | quantity | purchase price | total cost |
      | Milk      | 5        | 20000          | 100000     |
    And the remaining budget is 0

  Scenario: Cap the first purchase at the stock shortage and use the remaining budget
    Given the purchasing budget is 95000
    And the inventory and latest five purchase prices are:
      | item ID | item name | stock | target stock | price 1 | price 2 | price 3 | price 4 | price 5 |
      | 1       | Milk      | 8     | 10           | 30000   | 30000   | 30000   | 30000   | 20000   |
      | 2       | Tea       | 1     | 10           | 12000   | 12000   | 12000   | 12000   | 10000   |
    When the manager requests a restocking recommendation
    Then the recommendation contains these purchases in order:
      | item name | quantity | purchase price | total cost |
      | Milk      | 2        | 20000          | 40000      |
      | Tea       | 5        | 10000          | 50000      |
    And the remaining budget is 5000
    And no recommended purchase would raise stock above its target

  Scenario: Skip an unaffordable candidate and consider the next candidate
    Given the purchasing budget is 15000
    And the inventory and latest five purchase prices are:
      | item ID | item name | stock | target stock | price 1 | price 2 | price 3 | price 4 | price 5 |
      | 1       | Milk      | 0     | 5            | 30000   | 30000   | 30000   | 30000   | 20000   |
      | 2       | Tea       | 0     | 5            | 12000   | 12000   | 12000   | 12000   | 10000   |
    When the manager requests a restocking recommendation
    Then the recommendation contains only 1 unit of Tea at a total cost of 10000
    And the remaining budget is 5000

  Scenario: Exclude items without a positive discount or without a stock shortage
    Given the purchasing budget is 100000
    And the inventory and latest five purchase prices are:
      | item ID | item name | stock | target stock | price 1 | price 2 | price 3 | price 4 | price 5 |
      | 1       | Milk      | 2     | 10           | 10000   | 10000   | 10000   | 10000   | 20000   |
      | 2       | Tea       | 2     | 10           | 10000   | 10000   | 10000   | 10000   | 10000   |
      | 3       | Sugar     | 10    | 10           | 15000   | 15000   | 15000   | 15000   | 10000   |
      | 4       | Coffee    | 12    | 10           | 15000   | 15000   | 15000   | 15000   | 10000   |
    When the manager requests a restocking recommendation
    Then the recommendation contains no purchases
    And the remaining budget is 100000
    And no sale or disposal of existing stock is recommended

  Scenario Outline: Exclude an item with a non-positive current purchase price
    Given the purchasing budget is 100000
    And Milk has 0 units in stock and a target stock of 10 units
    And its latest five purchase prices are 10000, 10000, 10000, 10000, and <current price>
    When the manager requests a restocking recommendation
    Then Milk is excluded from the recommendation
    And the remaining budget is 100000

    Examples:
      | current price |
      | 0             |
      | -1000         |

  Scenario: Exclude an item without five purchase-price observations
    Given the purchasing budget is 100000
    And Milk has 0 units in stock and a target stock of 10 units
    And Milk has only four purchase-price observations
    When the manager requests a restocking recommendation
    Then Milk is excluded from the recommendation
    And the manager is informed that Milk has insufficient purchase-price history
    And the remaining budget is 100000

  Scenario: Resolve equal relative discounts consistently
    Given the purchasing budget is 10000
    And the inventory and latest five purchase prices are:
      | item ID | item name | stock | target stock | price 1 | price 2 | price 3 | price 4 | price 5 |
      | 2       | Tea       | 0     | 1            | 15000   | 15000   | 15000   | 15000   | 10000   |
      | 1       | Milk      | 0     | 1            | 15000   | 15000   | 15000   | 15000   | 10000   |
    When the manager requests a restocking recommendation
    Then the recommendation contains only 1 unit of Milk at a total cost of 10000
    And the remaining budget is 0

  Scenario: Preserve decimal precision when calculating affordable quantities
    Given the purchasing budget is 0.30
    And Milk has 0 units in stock and a target stock of 3 units
    And its latest five purchase prices are 0.20, 0.20, 0.20, 0.20, and 0.10
    When the manager requests a restocking recommendation
    Then the recommendation contains 3 units of Milk at a total cost of 0.30
    And the remaining budget is exactly 0.00

  Scenario: Return an empty recommendation when the budget is zero
    Given the purchasing budget is 0
    And there are discounted items below their target stock
    When the manager requests a restocking recommendation
    Then the recommendation contains no purchases
    And the remaining budget is 0

  Scenario: Reject a negative purchasing budget
    Given the purchasing budget is -1000
    When the manager requests a restocking recommendation
    Then the request is rejected with the message "Purchasing budget must not be negative."
    And no restocking recommendation is created

  Scenario: Generating a recommendation does not execute purchases
    Given the purchasing budget is 100000
    And Milk has 2 units in stock and a target stock of 7 units
    And its latest five purchase prices are 30000, 30000, 30000, 30000, and 20000
    When the manager requests a restocking recommendation
    Then the recommendation contains 5 units of Milk at a total cost of 100000
    And Milk still has 2 units in stock
    And no payment is recorded
