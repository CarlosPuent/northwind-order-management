# Northwind Order Management System

> RSM Training Program — Final Project · Carlos Puente

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Vue 3](https://img.shields.io/badge/Vue_3-4FC08D?style=flat-square&logo=vue.js&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-125_passing-2D7A3E?style=flat-square)

A production-style order management system built on the Northwind dataset. Features real-time Google Maps address validation, QuestPDF invoice generation, an analytics dashboard, and a complete order lifecycle — from creation to shipment.

---

## Quick Start (Docker — recommended)

The entire stack runs with a single command. No SQL Server, .NET, or Node.js required — only Docker Desktop.

### Step 1 — Install Docker Desktop

Download and install from [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop). Make sure it is running before continuing.

### Step 2 — Clone the repository

```bash
git clone https://github.com/CarlosPuent/northwind-order-management.git
cd northwind-order-management
```

### Step 3 — Create the `.env` file

The `.env` file is not included in the repository. You must create it manually in the root folder with your Google Maps API key.

**Windows (PowerShell):**

```powershell
New-Item .env -ItemType File
Add-Content .env "GOOGLE_MAPS_API_KEY=your_key_here"
```

**Mac / Linux:**

```bash
echo "GOOGLE_MAPS_API_KEY=your_key_here" > .env
```

Or simply create a file named `.env` in the root folder with this content:

```
GOOGLE_MAPS_API_KEY=your_key_here
```

> Replace `your_key_here` with the API key provided by the project author.  
> The key must have **Geocoding API**, **Maps JavaScript API**, and **Maps Static API** enabled.  
> If you received a key from the author, use that — it already has everything configured.

### Step 4 — Start everything

```bash
docker compose up --build
```

Docker will automatically:

1. Start SQL Server 2022 in a container
2. Create the Northwind database and load all data (830+ orders, customers, products)
3. Create the `ShippingGeocodes` table and mark the EF Core migration as applied
4. Start the .NET 10 API
5. Build and serve the Vue 3 frontend via Nginx

**Wait ~2 minutes** for the database to initialize. You will see this in the logs when everything is ready:

```
db-1  | Database initialization complete.
api-1 | Now listening on: http://[::]:5281
```

Then open:

| Service  | URL                           |
| -------- | ----------------------------- |
| Frontend | http://localhost:9000         |
| API      | http://localhost:5281         |
| Swagger  | http://localhost:5281/swagger |

> SQL Server: `localhost:1433` · user `sa` · password `Northwind@2026!`

**To stop:** `docker compose down`  
**To reset everything (including database):** `docker compose down -v && docker compose up --build`

---

## Local Setup (without Docker)

**Requirements:** .NET 10 SDK · Node.js 22+ · SQL Server Express · Google Maps API Key

### 1. Database

Create a Northwind database in your SQL Server instance. The classic Northwind SQL script is available at [Microsoft's GitHub](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs).

After loading Northwind, create `backend/src/Northwind.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Northwind": "Server=YOUR_SERVER\\INSTANCE;Database=Northwind;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 2. Backend

```bash
cd backend

# Add Google Maps key
echo "GOOGLE_MAPS_API_KEY=your_key_here" > .env

# Apply migration (creates ShippingGeocodes table)
dotnet ef database update \
  --project src/Northwind.Infrastructure \
  --startup-project src/Northwind.Api

# Start API → http://localhost:5281
.\start-api.ps1
```

### 3. Frontend

```bash
cd frontend

# Create frontend .env for the map widget
echo "VITE_GOOGLE_MAPS_API_KEY=your_key_here" > .env

npm install
npx quasar dev   # → http://localhost:9000
```

### 4. Tests

```bash
cd backend
dotnet test      # 125 tests, 0 failures
```

---

## Features

| Feature                | Details                                                                            |
| ---------------------- | ---------------------------------------------------------------------------------- |
| **Order lifecycle**    | Create → Edit → Ship. Status derived from `ShippedDate` — no extra column needed   |
| **Address validation** | Google Maps Geocoding with 24h cache decorator (same interface, zero extra calls)  |
| **Delivery map**       | Validated addresses stored in `ShippingGeocodes` and shown on the dashboard map    |
| **PDF Invoice**        | QuestPDF — branded layout, line items, totals, and delivery location map thumbnail |
| **Analytics**          | Orders over time, shipments by country, top customers — all filterable by year     |
| **Export**             | Orders table exportable to Excel (SheetJS) and PDF print view                      |
| **Validation**         | Two-layer: FluentValidation at API boundary (400) + domain rules in service (422)  |
| **Draft recovery**     | New order form auto-saves to Pinia store and offers recovery on next visit         |

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

### Key Design Decisions

| Decision                                 | Why                                                                                                 |
| ---------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `Result<T, Error>` instead of exceptions | Failure modes are explicit in method signatures — controllers map errors to RFC 7807 ProblemDetails |
| `ShippingGeocodes` as a separate table   | Avoids modifying the legacy Northwind schema — respect for shared production databases              |
| `CachedGeocodingService` decorator       | Open/Closed principle — same `IGeocodingService` interface, transparent 24h cache                   |
| `Order` as aggregate root                | Lines only accessible via `AddLine`/`RemoveLine` — domain invariants enforced at all times          |
| `IsShipped` derived from `ShippedDate`   | Northwind has no status column — ShippedDate is the single source of truth                          |
| Two-layer validation                     | FluentValidation catches bad input at the API boundary (400); domain validates business rules (422) |

---

## API Reference

```
GET    /api/orders                           Paginated orders — filters: customerId, region, isShipped, fromDate, toDate
GET    /api/orders/{id}                      Order detail with product names resolved
POST   /api/orders                           Create order (geocode saved automatically if address was validated)
PUT    /api/orders/{id}                      Update pending order
DELETE /api/orders/{id}                      Delete pending order
POST   /api/orders/{id}/ship                 Mark as shipped — sets ShippedDate, requires shipperId

GET    /api/invoices/{orderId}               Generate branded PDF invoice (QuestPDF)

GET    /api/geocoding/validate               Validate address via Google Maps (cached 24h)

GET    /api/analytics/orders-over-time       Orders + revenue by month, filterable by year
GET    /api/analytics/shipments-by-region    Top 10 countries by order count, filterable by year
GET    /api/analytics/top-customers          Top N customers by revenue, filterable by year
GET    /api/analytics/available-years        Available years for the year filter dropdown
GET    /api/analytics/delivery-locations     Recently geocoded delivery addresses for the map widget

GET    /api/customers                        All customers
GET    /api/employees                        All employees
GET    /api/shippers                         All shippers
GET    /api/products/search?q=               Search active products by name
```

Full interactive documentation at `/swagger` when the API is running.

---

## Stack

**Backend:** ASP.NET Core 10 · EF Core 10 · SQL Server 2022 · FluentValidation · QuestPDF · xUnit · Moq

**Frontend:** Vue 3 · Quasar Framework · Pinia · ApexCharts · Axios · SheetJS

**Infrastructure:** Google Maps APIs (Geocoding, Static Maps, JavaScript API) · Docker · Docker Compose

---

## Author

**Carlos Puente** · [github.com/CarlosPuent](https://github.com/CarlosPuent)
