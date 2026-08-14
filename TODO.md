# Project TODO & Backlog

This document tracks the pending microservices, gateway routing, event-driven messaging features, and maintenance tasks for `aspnetrun-microservices`.

---

## 🚀 Phase 1: API Gateway Configuration (`Gateway.Yarp`) [COMPLETED]

- [x] **Configure YARP Reverse Proxy** in [Program.cs](file:///i:/aspnetrun-microservices/src/Gateway/Gateway.Yarp/Program.cs):
  - [x] Registered `builder.Services.AddReverseProxy().LoadFromConfig(...).AddServiceDiscoveryDestinationResolver()`
  - [x] Added `app.MapReverseProxy()` in the HTTP request pipeline
- [x] **Define Routes and Clusters** in [appsettings.json](file:///i:/aspnetrun-microservices/src/Gateway/Gateway.Yarp/appsettings.json):
  - [x] Route `/api/v1/products/{**catch-all}` -> `products-api`
  - [x] Route `/api/v1/inventory/{**catch-all}` -> `inventory-api`
  - [x] Route `/api/v1/cart/{**catch-all}` -> `cart-api`
  - [x] Route `/api/v1/orders/{**catch-all}` -> `order-api`
- [x] **Observability & Logging**:
  - [x] Created [AppCallContextMiddleware.cs](file:///i:/aspnetrun-microservices/src/Gateway/Gateway.Yarp/Observability/AppCallContextMiddleware.cs) and [GatewayRequestLoggingMiddleware.cs](file:///i:/aspnetrun-microservices/src/Gateway/Gateway.Yarp/Observability/GatewayRequestLoggingMiddleware.cs)
- [x] **Clean Up Template Scaffolding**:
  - [x] Removed `WeatherForecast.cs`
- [x] **Tests**:
  - [x] Created unit tests in [GatewayConfigurationTests.cs](file:///i:/aspnetrun-microservices/tests/UnitTests/Gateway.Yarp.Tests/GatewayConfigurationTests.cs) (All passing)

---

## 🏷️ Phase 2: Discount gRPC Microservice (`Discount.Grpc`) [COMPLETED]

- [x] **Define Protobuf Contract** ([discount.proto](file:///i:/aspnetrun-microservices/src/Services/Discount/Discount.Grpc/Protos/discount.proto)):
  - [x] Protobuf messages: `GetDiscountRequest`, `CouponModel`, `CreateDiscountRequest`, `UpdateDiscountRequest`, `DeleteDiscountRequest`, `DeleteDiscountResponse`
- [x] **Datastore & Persistence**:
  - [x] SQLite database integration using `Microsoft.Data.Sqlite` + `Dapper`
  - [x] Automatic table creation and default coupon data seeding via [DiscountDbInitializer.cs](file:///i:/aspnetrun-microservices/src/Services/Discount/Discount.Grpc/Data/DiscountDbInitializer.cs)
- [x] **Implement gRPC Service**:
  - [x] Replace legacy template `GreeterService` with [DiscountService.cs](file:///i:/aspnetrun-microservices/src/Services/Discount/Discount.Grpc/Services/DiscountService.cs)
  - [x] Implemented `GetDiscount`, `CreateDiscount`, `UpdateDiscount`, `DeleteDiscount` gRPC handlers
- [x] **Aspire & Service Discovery Integration**:
  - [x] Registered `IDiscountRepository` and `DiscountService` in [Program.cs](file:///i:/aspnetrun-microservices/src/Services/Discount/Discount.Grpc/Program.cs)
- [x] **Tests**:
  - [x] Created unit tests in [DiscountServiceTests.cs](file:///i:/aspnetrun-microservices/tests/UnitTests/Discount.Grpc.Tests/DiscountServiceTests.cs) (6 tests passing)

---

## 🛒 Phase 3: Cart Microservice (`Cart.Api`) [COMPLETED]

- [x] **Aspire Infrastructure**:
  - [x] Added Redis container resource (`cartdb`) in [AppHost.cs](file:///i:/aspnetrun-microservices/src/aspire/aspnetrun-microservices.AppHost/AppHost.cs)
- [x] **Vertical Slice Features** ([Cart.Api](file:///i:/aspnetrun-microservices/src/Services/Cart/Cart.Api)):
  - [x] `GetCart` endpoint & handler ([GetCartQuery.cs](file:///i:/aspnetrun-microservices/src/Services/Cart/Cart.Api/Features/Cart/GetCart/GetCartQuery.cs))
  - [x] `UpdateCart` endpoint & handler with `Discount.Grpc` client integration ([UpdateCartCommand.cs](file:///i:/aspnetrun-microservices/src/Services/Cart/Cart.Api/Features/Cart/UpdateCart/UpdateCartCommand.cs))
  - [x] `DeleteCart` endpoint & handler ([DeleteCartCommand.cs](file:///i:/aspnetrun-microservices/src/Services/Cart/Cart.Api/Features/Cart/DeleteCart/DeleteCartCommand.cs))
  - [x] `Checkout` feature & `BasketCheckoutIntegrationEvent` publishing ([CheckoutCartCommand.cs](file:///i:/aspnetrun-microservices/src/Services/Cart/Cart.Api/Features/Cart/Checkout/CheckoutCartCommand.cs))
- [x] **Clean Up Template Scaffolding**:
  - [x] Removed template `WeatherForecast.cs`
- [x] **Tests**:
  - [x] Unit tests built & passed in [CartFeatureTests.cs](file:///i:/aspnetrun-microservices/tests/UnitTests/Cart.Api.Tests/CartFeatureTests.cs) (5 tests passing)

---

## 📦 Phase 4: Order Microservice (`Order.Api`) [COMPLETED]

- [x] **Aspire Infrastructure & Datastore**:
  - [x] Added PostgreSQL database resource `orders` in [AppHost.cs](file:///i:/aspnetrun-microservices/src/aspire/aspnetrun-microservices.AppHost/AppHost.cs)
- [x] **Domain & CQRS Features** ([Order.Api](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api)):
  - [x] Domain models (`OrderEntity`, `OrderItem`, `OrderStatus`)
  - [x] `CreateOrder` command & Minimal API endpoint ([CreateOrderCommand.cs](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api/Features/Orders/CreateOrder/CreateOrderCommand.cs))
  - [x] `GetOrdersByUsername` query & endpoint ([GetOrdersQuery.cs](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api/Features/Orders/GetOrdersByUsername/GetOrdersQuery.cs))
  - [x] `GetOrderById` query & endpoint ([GetOrderByIdQuery.cs](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api/Features/Orders/GetOrderById/GetOrderByIdQuery.cs))
  - [x] `CancelOrder` command & endpoint ([CancelOrderCommand.cs](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api/Features/Orders/CancelOrder/CancelOrderCommand.cs))
- [x] **Clean Up Template Scaffolding**:
  - [x] Removed `WeatherForecast.cs`
- [x] **Tests**:
  - [x] Created 5 unit tests in [OrderFeatureTests.cs](file:///i:/aspnetrun-microservices/tests/UnitTests/Order.Api.Tests/OrderFeatureTests.cs) (All passing)

---

## 🔄 Phase 5: End-to-End Event-Driven Messaging & Consumers [COMPLETED]

- [x] **Kafka Consumers & Event Handlers**:
  - [x] Built `BasketCheckoutConsumer` and `BasketCheckoutIntegrationEventHandler` in [Order.Api/Messaging](file:///i:/aspnetrun-microservices/src/Services/Order/Order.Api/Messaging) consuming `cart.events.v1`
  - [x] Published `OrderCreatedIntegrationEvent` (`orders.events.v1`) upon order placement
  - [x] Built `OrderCreatedConsumer` and `OrderCreatedIntegrationEventHandler` in [Inventory.Api/Messaging](file:///i:/aspnetrun-microservices/src/Services/Inventory/Inventory.Api/Messaging) consuming `orders.events.v1`

---

## 🧹 Phase 6: Code Maintenance & Warning Fixes [COMPLETED]

- [x] **Clean Up Empty Directories**:
  - [x] Removed unused folder `src/Services/Identity/Auth.Api`
  - [x] Removed obsolete folder `src/Services/Logging/LogStore.Api`
- [x] **Fix Build Warnings**:
  - [x] Fixed non-nullable `ParamName` warning in [ValidationException.cs](file:///i:/aspnetrun-microservices/src/Shared/Common.SharedKernel/Exceptions/ValidationException.cs#L12)
  - [x] Updated obsolete `PostgreSqlBuilder` constructor usage in [PostgresFixture.cs](file:///i:/aspnetrun-microservices/tests/IntegrationTests/Inventory.Api.IntegrationTests/PostgresFixture.cs#L7)
  - [x] Resolved missing Aspire resource variable references (`productsApi`, `cartApi`, `orderApi`) in [AppHost.cs](file:///i:/aspnetrun-microservices/src/aspire/aspnetrun-microservices.AppHost/AppHost.cs)
