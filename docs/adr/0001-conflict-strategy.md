# Conflict Strategy ADR

Date: 2026-03-14

Decision: resolve eligible promotions with a deterministic greedy selector ordered by the requested strategy, then apply explicit combinability and overlap rules before final acceptance.

Why:
- `CustomerBestPrice` maximizes immediate customer discount.
- `MarginFirst` prefers candidates that preserve the strongest post-funding margin rate.
- `FundedPromotionPreferred` prefers promotions with higher manufacturer-funded value.
- `InventoryReduction` biases toward high-stock lines.
- `CampaignPriority` allows explicit campaign ordering.
- vNext requires stack control beyond simple line collision checks.

Consequences:
- The engine remains deterministic and explainable.
- Non-combinable promotions cannot stack with any already applied promotion and are rejected with `NonCombinableWithAppliedPromotion`.
- Combinable promotions may stack only when their affected line set does not overlap; overlapping candidates are rejected with `OverlappingItemsNotAllowed`.
- Disjoint combinable promotions can still apply in one quote.
