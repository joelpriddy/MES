# PA.MES (Manufacturing & E-Commerce Suite)

## Overview
PA.MES is a modular microservices-based system for managing small-batch manufacturing, inventory, sales, and customer ordering.  
It uses a **React frontend**, **C#/.NET 8 backend with gRPC microservices**, **Kafka** for event streaming, and **MySQL (Entity Framework Core, code-first)** for persistence.  
The solution is structured with clean layering, domain-driven models, and pluggable providers for payments and shipping.

---

## Key Decisions
- **Branding/Namespaces:** `PA.*`
- **Authentication:** ASP.NET Core Identity + external login via **Google, Facebook, Apple**.  
  JWT for service-to-service auth, gRPC-Web for client communication.
- **Payments:** Start with **PayPal**. Pluggable model for **Venmo** and **Cash App** support later.  
- **Barcodes:** Use **GS1 DataMatrix (2D barcode)** for internal labeling. Accept **UPC/EAN/GS1-128/QR** for inbound scanning.  
- **UX First Principle:** **Scan-first**. Inbound receiving, ingredient usage, manufacturing consumption, and shipping should rely on barcode scanning with minimal typing.  
- **Manufacturing:** Small-batch fondant products (5lb & 10lb, flavors like lemongrass and peppermint). BOM-based recipes, work orders, and ingredient consumption tracking.  
- **Sites:** Single-site today, multi-site ready.  
- **Customers:** **Retail** & **Wholesale** (pricing tiers).  
- **Shipping:** Pluggable provider model. MVP supports "Mark as Shipped." Adapters for DesktopShipper and Amazon shipping later.  
- **Reports & Stats:**  
  - Inventory by site (ingredients, manufactured goods, resale goods).  
  - Sales for the last N days (up to a year).  
  - Ingredient reorder projections (days-of-supply).  
  - Projected next-month sales ($ and units).  

---

## Features

### Customer Portal (React)
- Social login (Google, Facebook, Apple).  
- Browse catalog (manufactured & resale products).  
- Cart & checkout with PayPal (Venmo/CashApp later).  
- Order history, status tracking, and receipts.  

### Admin UI (React)
- **Receiving:** Scan inbound Sysco barcodes; auto-parse UPC/EAN → match product → receive.  
- **Manufacturing:**  
  - Create work orders.  
  - Scan & issue ingredients, log consumption.  
  - Record yield and completion.  
  - Print GS1 DataMatrix barcodes for finished goods.  
- **Inventory:**  
  - Stock visibility by site.  
  - Adjustments, transfers, cycle counts (scan-first).  
- **Sales:**  
  - Orders dashboard with payment/shipping status.  
  - Mark orders as shipped; generate labels later.  
- **Catalog:** Manage products, variants, BOMs, and pricing tiers.  
- **Reports:**  
  - Current inventory by site.  
  - Sales by day (last X days up to 1 year).  
  - Ingredient reorder projections.  
  - Next-month sales projections.  

### Microservices (C# / gRPC)
- **CatalogService**: Products, variants, BOMs, prices.  
- **InventoryService**: Stock, lots, transactions, reservations.  
- **ManufacturingService**: Work orders, ingredient issues, completions.  
- **OrderService**: Orders, allocations, status transitions.  
- **PaymentService**: Provider-agnostic payments (PayPal first).  
- **ShippingService**: Pluggable shipping; MVP "Mark as Shipped."  
- **CustomerService**: Profiles, roles, addresses, auth.  
- **Gateway/BFF**: REST + gRPC-Web aggregation for React apps.  

### Messaging (Kafka)
- Topics:  
  - `order.placed|paid|allocated|shipped|cancelled`  
  - `inventory.received|issued|adjusted|reserved|released`  
  - `manufacturing.wo.created|started|completed`  
  - `payment.authorized|captured|failed|refunded`  
- Used for cross-service communication and dashboard projections.  

### Identity & Roles
- ASP.NET Core Identity + external OAuth providers.  
- JWT issued at Gateway, validated in services.  
- Roles: `Admin`, `Manager`, `Fulfillment`, `Customer (Retail|Wholesale)`.  

### Barcodes
- **Inbound:** Parse UPC/EAN/GS1 from Sysco/vendor labels.  
- **Outbound:** Print GS1 DataMatrix labels with GTIN, Lot, Exp, Serial.  
- **Supported formats:** UPC, EAN, GS1-128, QR, DataMatrix.  

### Forecasting/Analytics
- **Reorder Dates:** Based on average daily ingredient usage.  
- **Projected Sales:** Based on rolling 90-day averages for next month.  

---

## Solution Structure (Visual Studio)

**Solution:** `PA.MES.sln`

```
/src
  /PA.Contracts.Abstractions
  /PA.Contracts.Models
  /PA.Contracts.Grpc
  /PA.Platform.Common
  /PA.Platform.Data
  /PA.Platform.Messaging
  /PA.Barcode

  /Services
    /Catalog (Domain, Data, Api, Tests)
    /Inventory (Domain, Data, Api, Tests)
    /Manufacturing (Domain, Data, Api, Tests)
    /Orders (Domain, Data, Api, Tests)
    /Payments (Domain, Providers, Data, Api, Tests)
    /Shipping (Domain, Providers, Data, Api, Tests)
    /Customers (Domain, Identity, Data, Api, Tests)
    /Gateway (Api, Tests)

/apps
  /Admin.UI (React, Vite, TS)
  /Portal.UI (React, Vite, TS)

/tests
  /PA.EndToEnd.Tests
```

---

## Initial MVP Backlog
1. **Auth & Portal:** Social login, catalog browsing, checkout with PayPal.  
2. **Catalog & Pricing:** Products/variants, BOMs, retail/wholesale pricing.  
3. **Inventory:** Receiving with scanning, adjustments, reports.  
4. **Orders:** Order placement, payments, shipping dashboard.  
5. **Manufacturing:** Work orders, ingredient usage, yield tracking, label printing.  
6. **Forecasting:** Ingredient reorders, next-month projections.  
7. **Shipping:** Mark shipped MVP; providers pluggable.  

