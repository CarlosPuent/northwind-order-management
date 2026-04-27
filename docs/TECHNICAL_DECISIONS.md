# Technical Decisions

This document explains the key architectural and technical decisions made in the Northwind Order Management System. Each decision includes the rationale and the trade-offs considered.

---

## 1. Clean Architecture (4 projects)

**Decision:** Separate the solution into Domain, Application, Infrastructure, and API projects with strict dependency direction.

**Why:** Clean Architecture forces the dependency rule — Domain depends on nothing, Application depends only on Domain, Infrastructure implements Domain abstractions. This makes `OrderService` testable without a database (mock the repository), and allows swapping EF Core for any other ORM without touching business logic.

**Trade-off:** More projects and files than a simple layered approach. For a system this size, it's slightly over-engineered — but it demonstrates the ability to work in production-grade codebases where this pattern is standard.

---

## 2. Result\<T, Error\> Pattern

**Decision:** Use a functional `Result<T, Error>` type instead of throwing exceptions for expected failures.

**Why:** Exceptions are for unexpected situations (DB unreachable, null reference bugs). An invalid address or a missing customer is a predictable outcome, not an exception. `Result<T>` makes failure modes part of the method signature — the compiler and the caller both know what can go wrong.

**Trade-off:** Slightly more verbose than try/catch at the call site. But the clarity and testability outweigh the verbosity.

---

## 3. ShippingGeocodes as a Separate Table

**Decision:** Store geocoding data (lat/lng, standardized address, place type, raw API response) in a new `ShippingGeocodes` table instead of adding columns to the `Orders` table.

**Why:** The Northwind database is a legacy schema shared with downstream consumers. Adding columns to a 30-year-old table risks breaking other systems that depend on its structure. A side table with a one-to-one foreign key is the safe migration path.

**Trade-off:** An extra JOIN when querying orders with their geocode. Negligible performance cost given the index on `OrderId`.

---

## 4. Cached Geocoding Decorator

**Decision:** Wrap `GoogleMapsGeocodingService` in a `CachedGeocodingService` that uses `IMemoryCache` with 24-hour sliding expiration.

**Why:** Google Maps API calls cost money and add ~500ms latency. Caching prevents paying twice for the same address and makes repeated lookups instantaneous. The decorator pattern keeps the cache invisible to consumers — they depend on `IGeocodingService` and don't know which implementation they're using.

**Trade-off:** Memory usage grows with unique addresses. Acceptable for this project's scale.

---

## 5. Money Value Object

**Decision:** Model monetary amounts as a `Money` record (amount + currency) instead of raw `decimal`.

**Why:** A raw decimal doesn't carry currency information and allows nonsensical operations (adding USD to EUR). `Money` enforces currency consistency and makes the domain model self-documenting. EF Core value converters handle the transparent conversion between `Money` and the `money` SQL column type.

**Trade-off:** Requires value converters in every EF Core configuration that touches a price column.

---

## 6. Order as Aggregate Root

**Decision:** `Order` is the aggregate root. `OrderLine` is an owned entity accessible only through `Order.AddLine()` and `Order.RemoveLine()`.

**Why:** This prevents external code from creating orphaned lines or violating business rules (negative quantities, mixed currencies, modifying shipped orders). The `Lines` collection is exposed as `IReadOnlyCollection` — nobody can call `.Add()` from outside.

**Trade-off:** No direct `DbSet<OrderLine>` — all line queries go through the Order aggregate, which means loading the full order to modify a single line.

---

## 7. QuestPDF for Invoice Generation

**Decision:** Use QuestPDF instead of HTML-to-PDF libraries (wkhtmltopdf, Puppeteer, etc.).

**Why:** QuestPDF uses a fluent C# API that produces pixel-perfect PDFs without external browser dependencies. The output quality is significantly higher than HTML-to-PDF approaches, and the API is more predictable (no CSS rendering differences across environments).

**Trade-off:** QuestPDF Community license is free for non-commercial use only. For a production commercial system, a paid license would be required.

---

## 8. Vue 3 + Quasar + Composition API

**Decision:** Use Quasar Framework with Vue 3 Composition API and JavaScript (no TypeScript).

**Why:** Quasar provides production-grade UI components (tables with server-side pagination, form inputs with validation, notifications, dialogs) out of the box. The Composition API is the modern standard for Vue 3. JavaScript was chosen because the project requirements explicitly state "no TS required" — saving setup time without sacrificing functionality.

**Trade-off:** No compile-time type checking on the frontend. Mitigated by clear naming conventions and prop validation.

---

## 9. ApexCharts for Dashboard

**Decision:** Use ApexCharts instead of Chart.js.

**Why:** ApexCharts provides better default aesthetics (tooltips, animations, color palettes) with less configuration. The vue3-apexcharts wrapper integrates cleanly with Vue 3's reactivity system.

**Trade-off:** Larger bundle size than Chart.js. Acceptable for a dashboard application where the charts are the primary feature.
