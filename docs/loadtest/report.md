# Load Test Report

Date: 2026-03-14

Scenario:
- 20 line items per request
- intended promotion catalog size around 100 active promotions
- endpoint under test: `POST /quotes`
- script: `docs/loadtest/quotes-loadtest.js`

Short result summary:
- The script is designed for a lightweight local smoke run at 10 VUs for 30 seconds.
- The pricing engine does all promotion explainability work in the request path, so SQL and serialization are the main cost centers outside the discount pipeline.
- For repeatable numbers, seed a catalog near 100 promotions before running k6.

Notes:
- Run against the Docker Compose stack or a local SQL Server instance.
- Increase VUs gradually after verifying the promotion catalog and DB sizing.
