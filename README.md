# Northwind Order Management System

A modern Order Management System built for Northwind Traders, integrating geo-location services for address validation, interactive maps, analytics dashboards, and branded PDF invoice generation.

> **RSM Internship — Final Project**

---

## Screenshots

### Orders List

Paginated order table with filters by customer, region, and year. Export to Excel and PDF directly from the toolbar.

### Order Form with Address Validation

Create and edit orders with real-time Google Maps address validation, interactive map preview, and dynamic line items with auto-calculated totals.

### Dashboard

Analytics dashboard with orders-over-time bar chart, shipments-by-country donut chart, and KPI cards.

### PDF Invoice

Branded PDF invoice generated with QuestPDF, including company header, line items table, totals, and delivery location map thumbnail.

---

## Tech Stack

| Layer            | Technology                      | Purpose                                                   |
| ---------------- | ------------------------------- | --------------------------------------------------------- |
| **Backend**      | ASP.NET Core (.NET 10)          | REST API with Clean Architecture                          |
| **ORM**          | Entity Framework Core 10        | Database access with Fluent API configuration             |
| **Database**     | SQL Server Express              | Northwind legacy database + new ShippingGeocodes table    |
| **Frontend**     | Vue 3 + Quasar Framework        | SPA with Composition API                                  |
| **Charts**       | ApexCharts (vue3-apexcharts)    | Dashboard visualizations                                  |
| **Maps**         | Google Maps APIs                | Address Validation, Geocoding, Embedded Maps, Static Maps |
| **PDF**          | QuestPDF                        | Branded invoice generation with map thumbnails            |
| **State**        | Pinia + persistedstate          | Store management with form draft recovery                 |
| **HTTP**         | Axios                           | API communication with error interceptor                  |
| **Excel Export** | SheetJS (xlsx)                  | Client-side Excel generation                              |
| **Testing**      | xUnit + Moq + AwesomeAssertions | Domain and service unit tests                             |

---

## Architecture

The backend follows **Clean Architecture** with strict dependency direction:

## API → Application → Domain ← Infrastructure

- **Domain** — Pure C# entities, value objects (`Money`, `Address`, `GeoCoordinates`), and the `Result<T, Error>` pattern. Zero framework dependencies.
- **Application** — Use cases, repository interfaces, `OrderService`, DTOs, and commands. Depends only on Domain.
- **Infrastructure** — EF Core DbContext, repository implementations, Google Maps client with cache decorator, QuestPDF invoice generator. Depends on Domain abstractions.
- **API** — ASP.NET Core controllers, DI configuration, ProblemDetails error mapping. Thin layer that translates HTTP to Application calls.

### Key Architectural Decisions

| Decision                                 | Rationale                                                                                                                          |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `Result<T, Error>` instead of exceptions | Makes failure modes explicit in method signatures. Controllers map errors to RFC 7807 ProblemDetails.                              |
| `ShippingGeocodes` as a separate table   | Avoids modifying the legacy Northwind schema. Demonstrates respect for shared production databases.                                |
| `CachedGeocodingService` decorator       | Same `IGeocodingService` interface, transparent cache. Reduces Google Maps API costs and latency.                                  |
| `Money` value object for prices          | Prevents raw decimal bugs. EF Core value converters handle the DB mapping transparently.                                           |
| `Address` as owned entity                | Maps Northwind's 5 ship columns (`ShipAddress`, `ShipCity`, etc.) to a single value object.                                        |
| `Order` as aggregate root                | Lines are only accessible through `Order.AddLine()` / `Order.RemoveLine()`. Encapsulated collection prevents invariant violations. |

---

## API Endpoints

| Method | Endpoint                                          | Description                       |
| ------ | ------------------------------------------------- | --------------------------------- |
| GET    | `/api/customers`                                  | List all customers                |
| GET    | `/api/customers/search?q=`                        | Search customers by name          |
| GET    | `/api/employees`                                  | List all employees                |
| GET    | `/api/shippers`                                   | List all shippers                 |
| GET    | `/api/products/search?q=`                         | Search active products            |
| GET    | `/api/orders?page=&pageSize=&customerId=&region=` | Paginated orders with filters     |
| GET    | `/api/orders/{id}`                                | Order detail with lines           |
| POST   | `/api/orders`                                     | Create order                      |
| PUT    | `/api/orders/{id}`                                | Update order                      |
| DELETE | `/api/orders/{id}`                                | Delete order                      |
| GET    | `/api/geocoding/validate?street=&city=&country=`  | Validate address with Google Maps |
| GET    | `/api/invoices/{orderId}`                         | Generate PDF invoice              |
| GET    | `/api/analytics/orders-over-time?year=`           | Orders grouped by month           |
| GET    | `/api/analytics/shipments-by-region`              | Top 10 countries by order count   |
| GET    | `/api/analytics/available-years`                  | Available years for filtering     |

---

## Prerequisites

Before running the project, ensure you have:

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Node.js 22.22+](https://nodejs.org/)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) with the Northwind database installed
- A [Google Maps API Key](https://console.cloud.google.com/) with these APIs enabled:
  - Address Validation API
  - Geocoding API
  - Maps JavaScript API
  - Maps Static API

---

## Setup Guide

### 1. Clone the repository

```bash
git clone https://github.com/CarlosPuent/northwind-order-management.git
cd northwind-order-management
```

### 2. Run with Docker (recommended)

The easiest way to run the entire stack:

```bash
# Set your Google Maps API key
echo "GOOGLE_MAPS_API_KEY=your_key_here" > .env

# Build and start all services
docker compose up --build

# Wait ~60 seconds for SQL Server to initialize and load Northwind data
```

Once running:
- **Frontend:** http://localhost:9000
- **Backend API:** http://localhost:5281/api/customers
- **SQL Server:** localhost:1433 (sa / Northwind@2026!)

To stop: `docker compose down`
To reset the database: `docker compose down -v && docker compose up --build`

### 3. Configure the backend

```bash
cd backend
```

Create `src/Northwind.Api/appsettings.Development.json` (copy from the example):

```bash
cp src/Northwind.Api/appsettings.Development.json.example src/Northwind.Api/appsettings.Development.json
```

Edit `appsettings.Development.json` with your connection string:

```json
{
  "ConnectionStrings": {
    "Northwind": "Server=YOUR_SERVER\\INSTANCE;Database=Northwind;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
  },
  "GoogleMaps": {
    "ApiKey": ""
  }
}
```

Create a `.env` file in the `backend/` directory for the Google Maps API key:

GOOGLE_MAPS_API_KEY=your_key_here

### 4. Apply the database migration

This creates only the new `ShippingGeocodes` table — existing Northwind tables are not modified:

```bash
dotnet ef database update --project src/Northwind.Infrastructure --startup-project src/Northwind.Api
```

### 5. Run the backend

```bash
.\start-api.ps1
```

Or manually:

```bash
dotnet run --project src/Northwind.Api
```

The API will be available at `http://localhost:5281`.

### 6. Install and run the frontend

```bash
cd ../frontend
npm install
npx quasar dev
```

The frontend will be available at `http://localhost:9000`.

---

## Running Tests

```bash
cd backend
dotnet test
```

To generate a coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Project Structure

northwind-order-management/
├── backend/
│ ├── src/
│ │ ├── Northwind.Api/ # ASP.NET Core REST API
│ │ │ └── Controllers/ # 7 controllers (Orders, Customers, etc.)
│ │ ├── Northwind.Application/ # Use cases, services, interfaces
│ │ │ ├── Abstractions/ # Repository + service interfaces
│ │ │ └── Orders/ # OrderService, commands, DTOs
│ │ ├── Northwind.Domain/ # Pure domain model
│ │ │ ├── Common/ # Result<T>, Error, Entity base
│ │ │ ├── Entities/ # Order, Customer, Product, etc.
│ │ │ └── ValueObjects/ # Money, Address, GeoCoordinates
│ │ └── Northwind.Infrastructure/ # EF Core, Google Maps, QuestPDF
│ │ ├── GoogleMaps/ # Geocoding + cache decorator
│ │ ├── Persistence/ # DbContext, configs, repos, migrations
│ │ └── Reporting/ # QuestPDF invoice generator
│ └── tests/
│ ├── Northwind.Application.Tests/
│ └── Northwind.Api.Tests/
├── frontend/ # Vue 3 + Quasar SPA
│ └── src/
│ ├── boot/ # Axios configuration
│ ├── layouts/ # Main layout with sidebar
│ ├── pages/ # Dashboard, Orders, Form, Detail
│ └── stores/ # Pinia stores
├── docs/ # Documentation assets
├── .env.example # Environment variables template
├── docker-compose.yml # Container orchestration (planned)
└── README.md # This file

---

## Technical Decisions Document

For a detailed explanation of every architectural decision, see [TECHNICAL_DECISIONS.md](docs/TECHNICAL_DECISIONS.md).

---

## Author

**Carlos Puente**
