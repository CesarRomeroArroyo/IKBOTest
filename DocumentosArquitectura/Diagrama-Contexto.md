# Diagrama de contexto

Vista de alto nivel de actores y sistema.

```mermaid
flowchart LR
    Cliente[Cliente]
    Proveedor[Proveedor]
    Sistema[Product Requests API\nASP.NET Core Web API]
    DB[(MySQL 8.4)]

    Cliente -->|JWT + HTTP/JSON| Sistema
    Proveedor -->|JWT + HTTP/JSON| Sistema
    Sistema -->|EF Core| DB
```
