# C4 Container Diagram

```mermaid
flowchart LR
    Shopper[Shopper]
    Api[PromoEngine Api]
    Engine[Pricing Engine]
    Sql[SQL Server]
    K6[k6 Load Test]

    Shopper --> Api
    K6 --> Api
    Api --> Engine
    Api --> Sql
    Engine --> Api
```
