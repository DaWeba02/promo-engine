# Conflict Strategy ADR

Date: 2026-03-14

Decision: resolve overlapping promotions with a greedy selector ordered by the requested strategy.

Why:
- `CustomerBestPrice` maximizes immediate discount.
- `MarginFirst` keeps higher post-discount margin first.
- `FundedPromotionPreferred` consumes funded campaigns before unfunded offers.
- `InventoryReduction` biases toward high stock lines.
- `CampaignPriority` allows explicit campaign ordering.

Consequences:
- The engine is deterministic and explainable.
- Overlapping promotions are rejected with `ConflictStrategyRejected`.
- Disjoint promotions can still apply in one quote.
