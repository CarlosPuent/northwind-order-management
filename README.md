# Northwind Order Management System

> RSM Traingin Program — Final Project · Carlos Puente

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Vue 3](https://img.shields.io/badge/Vue_3-4FC08D?style=flat-square&logo=vue.js&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-125_passing-2D7A3E?style=flat-square)

A production-style order management system built on the Northwind dataset. Features real-time Google Maps address validation, QuestPDF invoice generation, an analytics dashboard, and a complete order lifecycle — from creation to shipment.

---

## Quick Start (Docker)

The fastest way to run the full stack with a single command:

```bash
git clone https://github.com/CarlosPuent/northwind-order-management.git
cd northwind-order-management

# Add your Google Maps API key
echo "GOOGLE_MAPS_API_KEY=your_key_here" > .env

docker compose up --build
```

Wait ~60 seconds for SQL Server to initialize. Then open:

| Service  | URL                           |
| -------- | ----------------------------- |
| Frontend | http://localhost:9000         |
| API      | http://localhost:5281         |
| Swagger  | http://localhost:5281/swagger |

> SQL Server credentials: `localhost:1433` · user `sa` · password `Northwind@2026!`

---

## Local Setup (without Docker)

**Requirements:** .NET 10 SDK · Node.js 22+ · SQL Server Express · Google Maps API Key

### Backend

```bash
cd backend

# 1. Create appsettings
cp src/Northwind.Api/appsettings.Development.json.example \
   src/Northwind.Api/appsettings.Development.json
# Edit the file with your SQL Server connection string

# 2. Add Google Maps key
echo "GOOGLE_MAPS_API_KEY=your_key_here" > .env

# 3. Apply migration (creates ShippingGeocodes table only)
dotnet ef database update \
  --project src/Northwind.Infrastructure \
  --startup-project src/Northwind.Api

# 4. Start API → http://localhost:5281
.\start-api.ps1
```

### Frontend

```bash
cd frontend
npm install
npx quasar dev   # → http://localhost:9000
```

### Tests

```bash
cd backend
dotnet test      # 125 tests, 0 failures
```

---

## Features

| Feature                | Details                                                                           |
| ---------------------- | --------------------------------------------------------------------------------- |
| **Order lifecycle**    | Create → Edit → Ship. Status derived from `ShippedDate` — no extra column needed  |
| **Address validation** | Google Maps Geocoding with 24h cache decorator (same interface, zero extra calls) |
| **PDF Invoice**        | QuestPDF — branded layout, line items, totals, and delivery map thumbnail         |
| **Analytics**          | Orders over time, shipments by country, top customers — all filterable by year    |
| **Export**             | Orders table exportable to Excel (SheetJS) and PDF print view                     |
| **Validation**         | Two-layer: FluentValidation at API boundary (400) + domain rules in service (422) |
| **Draft recovery**     | New order form auto-saves to Pinia store and offers recovery on next visit        |

---

## Architecture

Clean Architecture — strict dependency direction:

```
API → Application → Domain ← Infrastructure
```

- **Domain** — Entities, value objects (`Money`, `Address`, `GeoCoordinates`), `Result<T, Error>` pattern. Zero framework dependencies.
- **Application** — `OrderService`, commands, DTOs, repository interfaces, FluentValidation validators.
- **Infrastructure** — EF Core, repositories, Google Maps client + cache decorator, QuestPDF generator.
- **API** — ASP.NET Core controllers, FluentValidation auto-validation, ProblemDetails error mapping.

### Key Decisions

| Decision                                 | Why                                                                    |
| ---------------------------------------- | ---------------------------------------------------------------------- |
| `Result<T, Error>` instead of exceptions | Failure modes are explicit in method signatures                        |
| `ShippingGeocodes` as a separate table   | Avoids modifying the legacy Northwind schema                           |
| `CachedGeocodingService` decorator       | Open/Closed principle — same interface, transparent cache              |
| `Order` as aggregate root                | Lines only accessible via `AddLine`/`RemoveLine` — invariants enforced |
| `IsShipped` derived from `ShippedDate`   | Northwind has no status column — ShippedDate is the source of truth    |

---

## API Reference

```
GET    /api/orders                          Paginated orders (customerId, region, isShipped, fromDate, toDate)
GET    /api/orders/{id}                     Order detail with product names resolved
POST   /api/orders                          Create order (geocode saved if address was validated)
PUT    /api/orders/{id}                     Update pending order
DELETE /api/orders/{id}                     Delete pending order
POST   /api/orders/{id}/ship                Mark as shipped — sets ShippedDate, requires shipperId

GET    /api/invoices/{orderId}              Generate branded PDF invoice

GET    /api/geocoding/validate              Validate address via Google Maps

GET    /api/analytics/orders-over-time      Orders + revenue grouped by month, filterable by year
GET    /api/analytics/shipments-by-region   Top 10 countries by order count, filterable by year
GET    /api/analytics/top-customers         Top N customers by revenue, filterable by year
GET    /api/analytics/available-years       Years available for filtering

GET    /api/customers                       All customers
GET    /api/employees                       All employees
GET    /api/shippers                        All shippers
GET    /api/products/search?q=              Search active products
```

Full interactive documentation available at `/swagger` when the API is running.

---

## Stack

**Backend:** ASP.NET Core 10 · EF Core 10 · SQL Server · FluentValidation · QuestPDF · xUnit · Moq

**Frontend:** Vue 3 · Quasar · Pinia · ApexCharts · Axios · SheetJS

**Infrastructure:** Google Maps APIs · Docker · Docker Compose

---

## Author

**Carlos Puente** · [github.com/CarlosPuent](https://github.com/CarlosPuent)
