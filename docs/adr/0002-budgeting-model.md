# Budgeting Model ADR

Date: 2026-03-14

Decision: treat promotion budget usage as the total customer discount consumed at quote time, with total, daily, and per-customer buckets enforced before persistence.

Why:
- Budget exhaustion is easy to reason about when measured as discount value.
- `/quotes` can persist audit, redemption, and budget updates transactionally.
- `/simulate` and `/simulate/compare` stay side-effect free while still checking current stored budget usage.
- vNext requires both operational daily control and per-customer throttling.

Consequences:
- Budget checks happen against three caps: total promotion cap, UTC-day cap, and per-customer UTC-day cap.
- Daily and per-customer usage is stored in `BudgetConsumptions` keyed by promotion and UTC date, with an optional customer key for customer-specific buckets.
- `BudgetTotalExceeded`, `BudgetDailyExceeded`, and `BudgetPerCustomerExceeded` are emitted before persistence when remaining budget is insufficient.
- `PromotionRedemptions` remains the quote-level redemption ledger; `BudgetConsumptions` is the aggregated budget bucket table.
