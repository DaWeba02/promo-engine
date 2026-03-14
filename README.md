# PromoEngine

PromoEngine is a .NET 10 pricing and promotion engine built as a small Clean Architecture solution. It exposes a minimal API for promotion CRUD, quote generation, and dry-run simulation, with SQL Server persistence and explainable pricing decisions.

## Highlights

- Targets `net10.0` across all projects.
- SDK is pinned in [`global.json`](./global.json) to `10.0.200` with `rollForward: latestFeature`.
- Clean split across Domain, Application, Infrastructure, and API.
- SQL Server persistence via EF Core migrations.
- Unit tests plus Docker-backed integration tests using Testcontainers SQL Server.
- Serilog request logging, FluentValidation, ProblemDetails error handling, Swagger in `Development`.

## What It Implements

### Promotion types

Implemented in code:

- `PercentDiscount`
- `FixedAmountDiscount`
- `CartDiscount`
- `QuantityDeal`
- `Bundle`
- `Coupon`

### Conflict strategies

Implemented in code:

- `CustomerBestPrice`
- `MarginFirst`
- `FundedPromotionPreferred`
- `InventoryReduction`
- `CampaignPriority`

### Explainability

Each quote response contains a `promotions` collection with both applied and rejected decisions. Each item includes:

- `status`
- `reasonCode`
- `affectedItems`
- `discountAmount`
- `budgetImpact`
- `kpiEffect`

This is the primary explainability surface for debugging and downstream UI/reporting.

## API

### Endpoints

- `POST /quotes`
- `POST /simulate`
- `GET /promotions`
- `GET /promotions/{id}`
- `POST /promotions`
- `PUT /promotions/{id}`
- `DELETE /promotions/{id}`
- `GET /health/live`
- `GET /health/ready`
- `GET /ping`

### Health semantics

- `/health/live`: process liveness only.
- `/health/ready`: readiness including the EF Core SQL Server check.
- `/ping`: lightweight ping endpoint returning `status` and `utcNow`.

### Swagger

Swagger UI is available only in `Development`.

- UI: `/swagger`
- OpenAPI document: `/swagger/v1/swagger.json`

## Request Schemas

The API uses camelCase JSON.

**Important:** request payloads use **integer enum values** for fields like `type`, `discountValueType`, and `strategy`.  
Responses serialize enum values as **strings** (for example, `"CustomerBestPrice"`).

To see the exact numeric mappings, check the enum definitions in Swagger UI under **Schemas** or the OpenAPI document.

### `POST /promotions`

Request shape:

- `code`
- `name`
- `description`
- `campaignKey`
- `type` (int enum)
- `isActive`
- `startsAtUtc`
- `endsAtUtc`
- `priority`
- `isFunded`
- `budgetCap`
- `budgetConsumed`
- `value`
- `discountValueType` (int enum)
- `thresholdAmount`
- `requiredQuantity`
- `chargedQuantity`
- `bundlePrice`
- `minimumMarginRate`
- `couponCode`
- `targetSkus`
- `bundleSkus`

Example (uses integer enum values):

```json
{
  "code": "SPRING_COLA10",
  "name": "Spring Cola 10",
  "description": "10 percent off COLA_05",
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
  "discountValueType": 0,
  "thresholdAmount": 0,
  "requiredQuantity": 0,
  "chargedQuantity": 0,
  "bundlePrice": 0,
  "minimumMarginRate": 0.1,
  "couponCode": null,
  "targetSkus": ["COLA_05"],
  "bundleSkus": []
}
```

Response shape: `PromotionDto`, which adds `id` and echoes the stored fields above.

### `POST /quotes`

Request shape:

- `customerId`
- `currency`
- `couponCode`
- `strategy` (int enum)
- `minimumMarginRate`
- `items`
- `items[].sku`
- `items[].quantity`
- `items[].unitPrice`
- `items[].unitCost`
- `items[].stockLevel`

Example (uses integer enum values):

```json
{
  "customerId": "retail-customer-42",
  "currency": "EUR",
  "couponCode": "COFFEE5",
  "strategy": 0,
  "minimumMarginRate": 0.1,
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
    },
    {
      "sku": "COFFEE_250",
      "quantity": 1,
      "unitPrice": 4.99,
      "unitCost": 2.15,
      "stockLevel": 35
    }
  ]
}
```

### `POST /simulate`

Uses the same request schema as `/quotes`. It runs the full pricing pipeline but does not persist quote audits or redemption data.

Example (uses integer enum values):

```json
{
  "customerId": "retail-customer-42",
  "currency": "EUR",
  "couponCode": null,
  "strategy": 1,
  "minimumMarginRate": 0.15,
  "items": [
    {
      "sku": "COLA_05",
      "quantity": 2,
      "unitPrice": 1.59,
      "unitCost": 0.72,
      "stockLevel": 120
    },
    {
      "sku": "WATER_15",
      "quantity": 4,
      "unitPrice": 0.89,
      "unitCost": 0.31,
      "stockLevel": 240
    },
    {
      "sku": "COFFEE_250",
      "quantity": 1,
      "unitPrice": 4.99,
      "unitCost": 2.15,
      "stockLevel": 35
    }
  ]
}
```

> Note: `strategy: 0` / `strategy: 1` are examples. Check Swagger **Schemas** for the exact mapping of strategy names to numbers.

### Quote response shape

`/quotes` and `/simulate` return `QuoteResponseDto`.

Short example (response enums are strings):

```json
{
  "quoteId": "1e0d2f73-1cb3-4d2e-8c95-2d17d6d3d4e6",
  "isSimulation": false,
  "currency": "EUR",
  "strategy": "CustomerBestPrice",
  "subtotal": 11.54,
  "discountTotal": 0.48,
  "netTotal": 11.06,
  "marginAmount": 5.93,
  "marginRate": 0.5362,
  "lines": [
    {
      "sku": "COLA_05",
      "quantity": 3,
      "unitPrice": 1.59,
      "unitCost": 0.72,
      "lineSubtotal": 4.77,
      "discountAmount": 0.48,
      "netLineTotal": 4.29
    }
  ],
  "promotions": [
    {
      "promotionCode": "SPRING_COLA10",
      "status": "Applied",
      "reasonCode": "Applied",
      "discountAmount": 0.48,
      "budgetImpact": 0.48,
      "affectedItems": [
        {
          "sku": "COLA_05",
          "quantity": 3,
          "discountAmount": 0.48
        }
      ],
      "kpiEffect": {
        "revenueDelta": -0.48,
        "marginDelta": -0.48,
        "inventoryScore": 360,
        "budgetUsage": 0.48
      }
    }
  ],
  "kpiSummary": {
    "revenueDelta": -0.48,
    "marginDelta": -0.48,
    "inventoryScore": 360,
    "budgetUsage": 0.48
  }
}
```

## Running Locally

Prerequisites:

- .NET SDK `10.0.200`
- SQL Server reachable from the API
- Docker running if you want integration tests or Docker Compose

The repo already pins the SDK via [`global.json`](./global.json).

### Local API run

1. Point the API at SQL Server:

```powershell
$env:ConnectionStrings__SqlServer = "Server=localhost,14333;Database=PromoEngine;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
```

2. Run the API:

```powershell
dotnet run --project .\src\PromoEngine.Api\PromoEngine.Api.csproj
```

3. In `Development`, open Swagger at the URL shown by `dotnet run` (typically `http://localhost:5231/swagger`).

### Docker Compose

```powershell
docker compose up --build
```

This starts:

- SQL Server
- the API on port `8080`

Then open:

- `http://localhost:8080/swagger` when `ASPNETCORE_ENVIRONMENT=Development`

## Testing

Run the full suite:

```powershell
dotnet test PromoEngine.sln -c Release /m:1
```

Notes:

- Unit tests cover domain and application logic.
- Integration tests use Testcontainers SQL Server.
- **Docker Desktop must be running** for integration tests to execute.
- On Windows, solution-wide parallel builds/tests can hit transient file locking in `obj/Release`; use `/m:1`.

## Load Test

k6 script:

- [`docs/loadtest/quotes-loadtest.js`](./docs/loadtest/quotes-loadtest.js)

Run it against a running API:

```powershell
k6 run .\docs\loadtest\quotes-loadtest.js
```

Report:

- [`docs/loadtest/report.md`](./docs/loadtest/report.md)

## Configuration

### Database

Primary runtime override:

- `ConnectionStrings__SqlServer`

Example:

```powershell
$env:ConnectionStrings__SqlServer = "Server=localhost,14333;Database=PromoEngine;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
```

### Auth / dev secrets

There is no JWT, API key, or other application auth secret configured in the current codebase. The API is currently focused on pricing/promotion workflows and infrastructure wiring.

If you want to add authentication and authorization, reuse the patterns from the ServiceStarter templates (JWT bearer, policies, and integration-test coverage).

## Project Structure

### Source

- `src/PromoEngine.Api` - minimal API, validation, error handling, health checks, Swagger
- `src/PromoEngine.Application` - use-case orchestration, DTOs, repository abstractions
- `src/PromoEngine.Domain` - pricing pipeline, promotion evaluation, strategies, explainability
- `src/PromoEngine.Infrastructure` - EF Core DbContext, repositories, SQL Server migrations

### Tests

- `tests/PromoEngine.Domain.UnitTests` - promotion logic, strategy, rounding, margin, budget behavior
- `tests/PromoEngine.Application.UnitTests` - quote service orchestration and persistence side effects
- `tests/PromoEngine.IntegrationTests` - API flow, migrations, health endpoints via Testcontainers SQL Server

## CI

GitHub Actions workflow:

- [`.github/workflows/ci.yml`](./.github/workflows/ci.yml)

It restores, builds, and tests the solution with .NET `10.0.200`.

## Additional Docs

- ADRs: [`docs/adr/0001-conflict-strategy.md`](./docs/adr/0001-conflict-strategy.md), [`docs/adr/0002-budgeting-model.md`](./docs/adr/0002-budgeting-model.md), [`docs/adr/0003-persistence-model.md`](./docs/adr/0003-persistence-model.md), [`docs/adr/0004-explainability-format.md`](./docs/adr/0004-explainability-format.md)
- Examples: [`docs/examples/promotion-create.json`](./docs/examples/promotion-create.json), [`docs/examples/quote-request.json`](./docs/examples/quote-request.json)
- C4 container view: [`docs/c4-container.md`](./docs/c4-container.md)
