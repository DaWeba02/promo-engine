# Explainability Format ADR

Date: 2026-03-14

Decision: every promotion in evaluation emits one explainability record, whether it is applied or rejected.

Fields:
- `status`
- `reasonCode`
- `affectedItems`
- `discountAmount`
- `budgetImpact`
- `kpiEffect`

`kpiEffect` includes:
- `revenueDelta`
- `marginDelta`
- `inventoryScore`
- `budgetUsage`
- `manufacturerFundingAmount`
- `retailerFundingAmount`

Consequences:
- Applied and rejected promotions share one response shape.
- API consumers do not need a second diagnostics endpoint.
- Reason codes remain stable integration points for UI and analytics.
- vNext adds first-class rejection reasons for channel and segment filtering, combinability and overlap blocking, and budget guardrails, including `ChannelMismatch`, `SegmentMismatch`, `NonCombinableWithAppliedPromotion`, `OverlappingItemsNotAllowed`, `BudgetTotalExceeded`, `BudgetDailyExceeded`, `BudgetPerCustomerExceeded`, `MinCartValueNotMet`, `MaxDiscountExceeded`, and `MarginGuardRejected`.
