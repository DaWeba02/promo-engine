# Explainability Format ADR

Date: 2026-03-14

Decision: every promotion in evaluation emits one explainability record.

Fields:
- `status`
- `reasonCode`
- `affectedItems`
- `discountAmount`
- `budgetImpact`
- `kpiEffect`

Consequences:
- Applied and rejected promotions share one response shape.
- API consumers do not need a second diagnostics endpoint.
- Reason codes are stable integration points for UI and analytics.
