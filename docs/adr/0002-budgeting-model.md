# Budgeting Model ADR

Date: 2026-03-14

Decision: treat promotion budget usage as discount value consumed at quote time.

Why:
- Budget exhaustion is easy to reason about.
- Quote persistence can update both the redemption ledger and the promotion aggregate.
- Simulations stay side-effect free.

Consequences:
- Applied promotions with a cap or funded flag consume budget.
- `BudgetExceeded` is emitted before persistence when remaining budget is insufficient.
- `PromotionRedemptions` is the audit trail for budget burn.
