[![ci](https://github.com/DaWeba02/promo-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/DaWeba02/promo-engine/actions/workflows/ci.yml)
 
 # PromoEngine

PromoEngine is a .NET 10 pricing and promotions engine built as a small Clean Architecture solution. It exposes minimal API endpoints for promotion CRUD, quote generation, single-strategy dry-run simulation, and multi-strategy simulation comparison, with SQL Server persistence for promotions, quote audits, redemptions, and budget consumption.

## Highlights

- Targets `net10.0` with SDK pinned in [`global.json`](./global.json) to `10.0.200`
- Clean split across Domain, Application, Infrastructure, and API
- All pricing logic lives in Domain with no EF Core or ASP.NET dependencies
- Explainability is returned inline for both applied and rejected promotions
- `/quotes` persists side effects; `/simulate` and `/simulate/compare` are dry-run only
- Swagger UI and OpenAPI documents are available in `Development`

## What It Implements

### Promotion types

- `PercentDiscount`
- `FixedAmountDiscount`
- `CartDiscount`
- `QuantityDeal`
- `Bundle`
- `Coupon`

### Conflict strategies

- `CustomerBestPrice`
- `MarginFirst`
- `FundedPromotionPreferred`
- `InventoryReduction`
- `CampaignPriority`

### vNext pricing behavior

- Channels: `Store`, `Online`, `MobileApp`, `ClickAndCollect`
- Customer segments: `NewCustomer`, `ExistingCustomer`, `Loyalty`, `PriceSensitive`, `B2B`, `B2C`
- Promotion eligibility can be restricted by channel and segment; null means all
- Non-combinable promotions cannot stack with any already applied promotion
- Combinable promotions may stack only when their affected line set does not overlap
- Total budget cap, daily budget cap, and per-customer budget cap are enforced
- Daily and per-customer budget usage is tracked per promotion and per UTC day
- Funding split is modeled as manufacturer share plus retailer share
- `minimumCartValue` rejects a promotion before application when the cart is too small
- `maximumDiscount` rejects a promotion when the calculated discount exceeds the cap
- Bundle matching is satisfied when each bundle SKU is present, and bundles can apply multiple times based on minimum bundle SKU quantity
- Fixed amount discounts are capped per line so net line totals never go below zero

### Funding and KPI semantics

- `budgetUsage` is the total customer discount
- `manufacturerFundingAmount` and `retailerFundingAmount` show how that discount is split
- `marginDelta` reflects the retailer-funded portion only
- `revenueDelta` reflects the full customer discount

## API

### Endpoints

- `GET /promotions`
- `GET /promotions/{id}`
- `POST /promotions`
- `PUT /promotions/{id}`
- `DELETE /promotions/{id}`
- `POST /quotes`
- `POST /simulate`
- `POST /simulate/compare`
- `GET /health/live`
- `GET /health/ready`
- `GET /ping`

### Health semantics

- `/health/live`: process liveness only
- `/health/ready`: readiness including the EF Core SQL Server check
- `/ping`: lightweight health probe returning `status` and `utcNow`

### Swagger and OpenAPI

Swagger UI is available only in `Development`.

- Swagger UI: `/swagger`
- Swagger JSON: `/swagger/v1/swagger.json`
- OpenAPI JSON: `/openapi/v1.json`

## Request Schemas

The API uses camelCase JSON.

Contract detail:

- Request payloads use integer enum values
- Response payloads serialize enum values as strings
- This applies to request enum fields such as `type`, `discountValueType`, `strategy`, `channel`, `segment`, and `strategies[]`
- See Swagger Schemas for numeric mappings

### `POST /promotions`

The promotion upsert request includes the MVP fields plus these vNext fields:

- `channel`
- `segment`
- `isCombinable`
- `budgetDailyCap`
- `budgetPerCustomerCap`
- `minimumCartValue`
- `maximumDiscount`
- `fundingManufacturerRate`
- `fundingRetailerRate`

`isFunded` is still accepted for backward compatibility. If funding rates are omitted and `isFunded` is `true`, the API derives a `0.5 / 0.5` manufacturer/retailer split.

Example:

```json
{
  "code": "SPRING_COLA10",
  "name": "Spring Cola 10",
  "description": "10 percent off COLA_05 for online existing customers",
  "campaignKey": "SPRING-2026",
  "type": 0,
  "isActive": true,
  "startsAtUtc": "2026-03-14T00:00:00Z",
  "endsAtUtc": "2026-06-30T23:59:59Z",
  "priority": 100,
  "isFunded": true,
  "budgetCap": 5000,
  "budgetConsumed": 0,
  "value": 10,
  "discountValueType": 1,
  "thresholdAmount": 0,
  "requiredQuantity": 0,
  "chargedQuantity": 0,
  "bundlePrice": 0,
  "minimumMarginRate": 0.1,
  "couponCode": null,
  "targetSkus": ["COLA_05"],
  "bundleSkus": [],
  "channel": 1,
  "segment": 1,
  "isCombinable": true,
  "budgetDailyCap": 750,
  "budgetPerCustomerCap": 50,
  "minimumCartValue": 25,
  "maximumDiscount": 20,
  "fundingManufacturerRate": 0.5,
  "fundingRetailerRate": 0.5
}
```

See [`docs/examples/promotion-create.json`](./docs/examples/promotion-create.json).

### `POST /quotes`

Quote requests include:

- `customerId`
- `currency`
- `couponCode`
- `strategy`
- `minimumMarginRate`
- `channel`
- `segment`
- `items`

If `channel` or `segment` are omitted, the service defaults to `Online` and `ExistingCustomer`.

Example:

```json
{
  "customerId": "retail-customer-42",
  "currency": "EUR",
  "couponCode": "SAVE20",
  "strategy": 0,
  "minimumMarginRate": 0.1,
  "channel": 1,
  "segment": 1,
  "items": [
    {
      "sku": "COLA_05",
      "quantity": 3,
      "unitPrice": 1.59,
      "unitCost": 0.72,
      "stockLevel": 120
    },
    {
      "sku": "WATER_15",
      "quantity": 2,
      "unitPrice": 0.89,
      "unitCost": 0.31,
      "stockLevel": 240
    }
  ]
}
```

See [`docs/examples/quote-request.json`](./docs/examples/quote-request.json).

### `POST /simulate`

`/simulate` uses the same request schema as `/quotes` and runs the same pricing pipeline, but it does not persist quote audits, redemptions, or budget consumption.

### `POST /simulate/compare`

`/simulate/compare` is a dry-run comparison endpoint. It replaces `strategy` with `strategies`, which is an array of integer enum values.

Example:

```json
{
  "customerId": "retail-customer-42",
  "currency": "EUR",
  "couponCode": null,
  "strategies": [0, 1, 2],
  "minimumMarginRate": 0.1,
  "channel": 1,
  "segment": 1,
  "items": [
    {
      "sku": "COLA_05",
      "quantity": 3,
      "unitPrice": 1.59,
      "unitCost": 0.72,
      "stockLevel": 120
    },
    {
      "sku": "WATER_15",
      "quantity": 2,
      "unitPrice": 0.89,
      "unitCost": 0.31,
      "stockLevel": 240
    }
  ]
}
```

See [`docs/examples/simulate-compare-request.json`](./docs/examples/simulate-compare-request.json).

## Explainability

Every evaluation returns one decision record per promotion with:

- `status`
- `reasonCode`
- `affectedItems`
- `discountAmount`
- `budgetImpact`
- `kpiEffect`

Important reason codes you will commonly see:

- `ChannelMismatch`
- `SegmentMismatch`
- `NonCombinableWithAppliedPromotion`
- `OverlappingItemsNotAllowed`
- `BudgetTotalExceeded`
- `BudgetDailyExceeded`
- `BudgetPerCustomerExceeded`
- `MinCartValueNotMet`
- `MaxDiscountExceeded`
- `MarginGuardRejected`

## Persistence Semantics

- `POST /quotes` persists quote audit, promotion redemptions, and budget consumption updates
- `POST /simulate` is dry-run only and does not persist anything
- `POST /simulate/compare` is dry-run only and does not persist anything
- Budget buckets are tracked by promotion and UTC date; per-customer buckets are also scoped to the same UTC date

## Running Locally

### Local API run

```powershell
$env:ConnectionStrings__SqlServer = "Server=localhost,14333;Database=PromoEngine;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
dotnet run --project .\src\PromoEngine.Api\PromoEngine.Api.csproj
```

In `Development`, open Swagger at the URL reported by `dotnet run`.

### Docker Compose

```powershell
$env:MSSQL_SA_PASSWORD = "Your_strong_Password123"
docker compose up --build
```

Compose uses `MSSQL_SA_PASSWORD` consistently and resolves `sqlcmd` from `/opt/mssql-tools*/bin/sqlcmd` for the SQL health check.

## Testing

```powershell
dotnet build PromoEngine.sln -c Release
dotnet test PromoEngine.sln -c Release
```

Notes:

- Unit tests cover channel/segment filtering, combinability, budget caps, funding split, fixed-amount capping, bundle matching, and min/max discount rules
- Integration tests cover CRUD, mismatch reasons, dry-run behavior, budget persistence behavior, and `/simulate/compare`
- Docker Desktop must be running for the integration suite

## Configuration

### Database

Primary runtime override:

- `ConnectionStrings__SqlServer`

Docker and CI use `MSSQL_SA_PASSWORD` for the SQL Server container.

## Project Structure

- `src/PromoEngine.Api` - minimal API endpoints, validation, Swagger, health checks
- `src/PromoEngine.Application` - orchestration, DTOs, ports
- `src/PromoEngine.Domain` - pricing logic, explainability, conflict handling
- `src/PromoEngine.Infrastructure` - EF Core DbContext, repositories, migrations
- `tests/PromoEngine.Domain.UnitTests` - engine behavior
- `tests/PromoEngine.Application.UnitTests` - orchestration and persistence behavior
- `tests/PromoEngine.IntegrationTests` - end-to-end API coverage

## Additional Docs

- ADRs: [`docs/adr/0001-conflict-strategy.md`](./docs/adr/0001-conflict-strategy.md), [`docs/adr/0002-budgeting-model.md`](./docs/adr/0002-budgeting-model.md), [`docs/adr/0003-persistence-model.md`](./docs/adr/0003-persistence-model.md), [`docs/adr/0004-explainability-format.md`](./docs/adr/0004-explainability-format.md)
- Examples: [`docs/examples/promotion-create.json`](./docs/examples/promotion-create.json), [`docs/examples/quote-request.json`](./docs/examples/quote-request.json), [`docs/examples/simulate-compare-request.json`](./docs/examples/simulate-compare-request.json)
- C4 container view: [`docs/c4-container.md`](./docs/c4-container.md)
- Load test: [`docs/loadtest/quotes-loadtest.js`](./docs/loadtest/quotes-loadtest.js), [`docs/loadtest/report.md`](./docs/loadtest/report.md)
