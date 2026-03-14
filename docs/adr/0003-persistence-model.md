# Persistence Model ADR

Date: 2026-03-14

Decision: persist promotions, quote audits, and promotion redemptions in SQL Server through EF Core.

Why:
- Promotions need durable CRUD storage.
- Quotes need an immutable audit record.
- Budget consumption needs a ledger for reconciliation.

Consequences:
- `/quotes` writes `QuoteAudits` and `PromotionRedemptions`.
- `/simulate` executes the same engine but persists nothing.
- EF Core migrations live under `src/PromoEngine.Infrastructure/Persistence/Migrations`.
