# OrderFlow - E-Commerce Order Management System

OrderFlow is a modern e-commerce platform built with **microservices architecture** using **.NET 10** for the backend and **React 19** for the frontend, orchestrated with **.NET Aspire**.

## Table of Contents

- [General Architecture](#general-architecture)
- [Project Structure](#project-structure)
- [Microservices](#microservices)
- [Communication Patterns](#communication-patterns)
- [Business Workflows](#business-workflows)
- [API Gateway](#api-gateway)
- [Frontend](#frontend)
- [Infrastructure](#infrastructure)
- [Observability](#observability)
- [Installation and Execution](#installation-and-execution)
- [Testing](#testing)
- [Technology Stack](#technology-stack)

---

## General Architecture

OrderFlow implements a **decoupled microservices architecture** with the **Database-per-Service** pattern. Each service has its own PostgreSQL database, ensuring complete data isolation.

```
┌─────────────────────────────────────────────────────────────────┐
│                      FRONTEND (React 19)                        │
│                  SPA Application - In development               │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ HTTP/JSON + JWT Auth
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               API GATEWAY (YARP) - Port 5000                    │
│  ┌─────────────┐ ┌──────────────┐ ┌────────────────────────┐   │
│  │Rate Limiting│ │JWT Validation│ │ Service Discovery      │   │
│  │   (Redis)   │ │ Centralized  │ │ (.NET Aspire)          │   │
│  └─────────────┘ └──────────────┘ └────────────────────────┘   │
└────────┬────────────────────┬────────────────────┬──────────────┘
         │                    │                    │
    ┌────▼────┐         ┌─────▼─────┐        ┌────▼─────┐
    │Identity │         │ Catalog   │        │ Orders   │
    │ :5001   │         │  :5002    │        │  :5003   │
    │         │◄───────►│           │◄──────►│          │
    └────┬────┘  HTTP   └─────┬─────┘  HTTP  └────┬─────┘
         │                    │                    │
    ┌────▼────┐         ┌─────▼─────┐        ┌────▼─────┐
    │identitydb│        │ catalogdb │        │ ordersdb │
    └─────────┘         └───────────┘        └──────────┘
         │                    │                    │
         └────────────────────┼────────────────────┘
                              │
                         PostgreSQL 16
                              │
              ┌───────────────┴───────────────┐
              │                               │
         ┌────▼────┐                    ┌─────▼──────┐
         │RabbitMQ │  ────────────────► │Notifications│
         │ Events  │                    │   Worker   │
         └─────────┘                    └────────────┘
                                              │
                                         ┌────▼────┐
                                         │  SMTP   │
                                         │(MailDev)│
                                         └─────────┘
```

### Design Principles

- **Database-per-Service**: Each microservice has its own PostgreSQL database
- **Event-Driven Architecture**: Asynchronous communication via RabbitMQ for non-critical operations
- **API Gateway Pattern**: Single entry point with centralized authentication
- **Service Discovery**: Automatic service discovery with .NET Aspire
- **Observability-First**: Integrated OpenTelemetry for traces, metrics, and logs

---

## Project Structure

```
Orderflow_Project_2025/
│
├── Orderflow.AppHost/               # .NET Aspire Orchestration
│   └── AppHost.cs                   # Services and infrastructure configuration
│
├── Orderflow.ServiceDefaults/       # Shared service configuration
│   └── Extensions.cs                # OpenTelemetry, Health Checks, etc.
│
├── Orderflow.Shared/                # Shared code between services
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Events/                      # Integration Events (RabbitMQ)
│   │   ├── UserRegisteredEvent.cs
│   │   ├── OrderCreatedEvent.cs
│   │   └── OrderCancelledEvent.cs
│   └── Extensions/                  # Shared extensions
│
├── Orderflow.API.Gateway/           # API Gateway (YARP Reverse Proxy)
│   ├── Program.cs                   # Gateway configuration
│   └── appsettings.json             # YARP routes and clusters
│
├── Orderflow.Identity/              # Authentication Microservice
│   ├── Controllers/                 # AuthController, UsersController
│   ├── Services/                    # AuthService, UserService, TokenService
│   ├── Data/                        # IdentityDbContext, Migrations
│   └── Program.cs
│
├── Orderflow.Catalog/               # Catalog Microservice
│   ├── Controllers/                 # ProductsController, CategoriesController
│   ├── Services/                    # ProductService, CategoryService
│   ├── Data/                        # CatalogDbContext, Entities
│   └── Program.cs
│
├── Orderflow.Orders/                # Orders Microservice
│   ├── Controllers/                 # OrdersController
│   ├── Services/                    # OrderService
│   ├── Clients/                     # CatalogClient (HTTP client)
│   ├── Data/                        # OrdersDbContext, Entities
│   └── Program.cs
│
├── Orderflow.Notifications/         # Notifications Worker Service
│   ├── Consumers/                   # MassTransit Consumers
│   │   ├── UserRegisteredConsumer.cs
│   │   ├── OrderCreatedConsumer.cs
│   │   └── OrderCancelledConsumer.cs
│   ├── Services/                    # EmailService
│   └── Program.cs
│
├── Orderflow.Web/                   # React Frontend (In development)
│
├── Orderflow.Api.Identity.Test/     # Identity Unit Tests
├── TestOrderflow.Console/           # Integration Tests
│
├── docker-compose.yaml              # Docker Infrastructure
├── Directory.Packages.props         # Centralized NuGet version management
└── ProyectoOrderflow.sln            # .NET Solution
```

---

## Microservices

### 1. Orderflow.Identity (Port 5001)

**Responsibility**: Authentication, authorization, and user management.

| Feature | Description |
|---------|-------------|
| JWT Authentication | Token generation and validation |
| ASP.NET Core Identity | User and role management |
| User CRUD | Administrative operations |
| Role Management | Admin, Customer |
| Account Locking | Security and access control |

**Main Endpoints:**
```
POST   /api/v1/auth/register           # Public registration
POST   /api/v1/auth/login              # Login → JWT Token
GET    /api/v1/users/me                # User profile (auth)
PUT    /api/v1/users/me                # Update profile (auth)
GET    /api/v1/admin/users             # List users (admin)
POST   /api/v1/admin/users/{id}/lock   # Lock user (admin)
```

**Database**: `identitydb`

---

### 2. Orderflow.Catalog (Port 5002)

**Responsibility**: Product, category, and inventory management.

| Feature | Description |
|---------|-------------|
| Product CRUD | Complete product management |
| Categories | Catalog organization |
| Stock Control | Real-time availability |
| Inventory Reservation | For order processing |

**Data Model:**
```
Category (1) ──── (N) Product (1) ──── (1) Stock
    │                     │                  │
    ├─ Id                 ├─ Id              ├─ QuantityAvailable
    ├─ Name               ├─ Name            ├─ QuantityReserved
    └─ Description        ├─ Price           └─ UpdatedAt
                          ├─ IsActive
                          └─ CategoryId
```

**Main Endpoints:**
```
GET    /api/v1/categories                    # List categories
GET    /api/v1/products                      # List products (paginated)
GET    /api/v1/products/{id}                 # Product details
POST   /api/v1/products/{id}/stock/reserve   # Reserve stock (internal)
POST   /api/v1/products/{id}/stock/release   # Release stock (internal)
```

**Database**: `catalogdb`

---

### 3. Orderflow.Orders (Port 5003)

**Responsibility**: Complete order lifecycle management.

| Feature | Description |
|---------|-------------|
| Order Creation | With stock validation |
| Order States | Controlled transitions |
| Cancellation | Automatic stock release |
| History | By user and administrator |

**Order States:**
```
Pending → Confirmed → Processing → Shipped → Delivered
   ↓           ↓
Cancelled  Cancelled
```

**Main Endpoints:**
```
POST   /api/v1/orders                        # Create order (auth)
GET    /api/v1/orders                        # My orders (auth)
GET    /api/v1/orders/{id}                   # Order details (auth)
POST   /api/v1/orders/{id}/cancel            # Cancel order (auth)
GET    /api/v1/admin/orders                  # All orders (admin)
PATCH  /api/v1/admin/orders/{id}/status      # Change status (admin)
```

**Database**: `ordersdb`

---

### 4. Orderflow.Notifications (Worker Service)

**Responsibility**: Asynchronous email notification processing.

| Feature | Description |
|---------|-------------|
| RabbitMQ Consumer | Listens to events with MassTransit |
| Email Sending | Via MailKit/SMTP |
| Automatic Retries | Policy: 1s, 5s, 15s, 30s |

**Processed Events:**
```
UserRegisteredEvent   → Welcome email
OrderCreatedEvent     → Order confirmation
OrderCancelledEvent   → Cancellation notification
```

**Does not expose HTTP endpoints** (it's a background worker)

---

## Communication Patterns

### Synchronous Communication (HTTP/REST)

Used for operations requiring immediate response and real-time validation.

```
┌──────────────┐          HTTP Request           ┌───────────────┐
│    Orders    │ ───────────────────────────────►│    Catalog    │
│   Service    │◄─────────────────────────────── │    Service    │
└──────────────┘          HTTP Response          └───────────────┘
```

**Use Cases:**
- Validate product existence
- Verify stock availability
- Reserve inventory for an order
- Release stock on cancellation

**Implementation:**
```csharp
// CatalogClient.cs in Orders Service
private readonly HttpClient _http = httpClientFactory.CreateClient("catalog");

// Configuration with Service Discovery
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("https+http://orderflow-catalog");
});
```

---

### Asynchronous Communication (RabbitMQ + MassTransit)

Used for decoupled operations, notifications, and background processing.

```
┌──────────────┐     Publish Event      ┌───────────┐     Consume      ┌───────────────┐
│   Identity   │ ──────────────────────►│           │─────────────────►│               │
│   Orders     │                        │ RabbitMQ  │                  │ Notifications │
│   Service    │                        │           │                  │    Worker     │
└──────────────┘                        └───────────┘                  └───────────────┘
```

**Integration Events:**
```csharp
// Orderflow.Shared/Events/
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
}

public record UserRegisteredEvent(string UserId, string Email, string FirstName);
public record OrderCreatedEvent(int OrderId, string UserId, IEnumerable<OrderItemEvent> Items);
public record OrderCancelledEvent(int OrderId, string UserId, string Reason);
```

**Advantages:**
- Complete decoupling between services
- Background processing
- Automatic retries with exponential backoff
- Fault tolerance

---

## Business Workflows

### 1. User Registration

```
┌─────────┐  POST /api/v1/auth/register  ┌────────────┐
│ Client  │─────────────────────────────►│API Gateway │
└─────────┘                              └─────┬──────┘
                                               │
                                               ▼
                                        ┌──────────────┐
                                        │   Identity   │
                                        │   Service    │
                                        └──────┬───────┘
                                               │
                    ┌──────────────────────────┼──────────────────────────┐
                    │                          │                          │
                    ▼                          ▼                          ▼
             ┌────────────┐           ┌──────────────┐           ┌──────────────┐
             │  Create in │           │   Generate   │           │   Publish    │
             │  identitydb│           │  JWT Token   │           │UserRegistered│
             └────────────┘           └──────────────┘           │   Event      │
                                                                 └──────┬───────┘
                                                                        │
                                                                        ▼
                                                                 ┌──────────────┐
                                                                 │Notifications │
                                                                 │   Worker     │
                                                                 └──────┬───────┘
                                                                        │
                                                                        ▼
                                                                 ┌──────────────┐
                                                                 │   Welcome    │
                                                                 │    Email     │
                                                                 └──────────────┘
```

---

### 2. Order Creation (Complete Workflow)

```
┌─────────┐  POST /api/v1/orders + JWT   ┌────────────┐
│ Client  │─────────────────────────────►│API Gateway │
└─────────┘                              └─────┬──────┘
                                               │ Validates JWT
                                               │ Rate Limit
                                               ▼
                                        ┌──────────────┐
                                        │   Orders     │
                                        │   Service    │
                                        └──────┬───────┘
                                               │
           ┌───────────────────────────────────┤
           │                                   │
           ▼                                   ▼
    ┌──────────────┐                  ┌──────────────────┐
    │   Validate   │  HTTP Request   │     Catalog      │
    │   Products   │────────────────►│     Service      │
    │              │◄────────────────│                  │
    └──────────────┘  Product Data   └─────────┬────────┘
                                               │
                                               ▼
                                      ┌──────────────────┐
                                      │  Reserve Stock   │
                                      │  in catalogdb    │
                                      └─────────┬────────┘
                                                │
                    ┌───────────────────────────┤
                    │                           │
                    ▼                           ▼
             ┌────────────┐            ┌──────────────────┐
             │ Create in  │            │    Publish       │
             │  ordersdb  │            │ OrderCreatedEvent│
             │            │            └────────┬─────────┘
             └────────────┘                     │
                                                ▼
                                        ┌───────────────┐
                                        │ Notifications │
                                        │    Worker     │
                                        └───────┬───────┘
                                                │
                                                ▼
                                        ┌───────────────┐
                                        │ Confirmation  │
                                        │    Email      │
                                        └───────────────┘
```

---

### 3. Order Cancellation

```
POST /api/v1/orders/{id}/cancel
              │
              ▼
┌──────────────────────────────────────────────────────────────┐
│                      Orders Service                           │
│                                                               │
│  1. Validate that the user is the order owner                │
│  2. Validate that the status allows cancellation             │
│     (Pending or Confirmed)                                    │
│                                                               │
└─────────────────────────────┬────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                     Catalog Service                           │
│                                                               │
│  3. Release reserved stock for each item                     │
│     QuantityReserved -= quantity                              │
│     QuantityAvailable += quantity                             │
│                                                               │
└─────────────────────────────┬────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                      Orders Service                           │
│                                                               │
│  4. Update status to "Cancelled"                             │
│  5. Publish OrderCancelledEvent to RabbitMQ                  │
│                                                               │
└─────────────────────────────┬────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                   Notifications Worker                        │
│                                                               │
│  6. Consume OrderCancelledEvent                              │
│  7. Send cancellation email                                  │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## API Gateway

The **API Gateway** is the single entry point for all client requests. Implemented with **YARP (Yet Another Reverse Proxy)**.

### Features

| Feature | Implementation |
|---------|----------------|
| Reverse Proxy | YARP 2.3.0 |
| Rate Limiting | Redis + Sliding Window |
| Authentication | Centralized JWT Bearer |
| Service Discovery | Automatic .NET Aspire |
| CORS | Configured for frontend |

### Authorization Policies

| Policy | Description | Routes |
|--------|-------------|--------|
| `anonymous` | No authentication | `/api/v1/auth/*`, `/api/v1/products/*`, `/api/v1/categories/*` |
| `authenticated` | Valid JWT required | `/api/v1/users/*`, `/api/v1/orders/*` |
| `admin` | JWT + Admin Role | `/api/v1/admin/*` |

### Route Configuration (YARP)

```yaml
# Identity Routes
/api/v1/auth/*              → Identity Service (anonymous)
/api/v1/users/*             → Identity Service (authenticated)
/api/v1/admin/users/*       → Identity Service (admin)

# Catalog Routes
/api/v1/categories/*        → Catalog Service (anonymous)
/api/v1/products/*          → Catalog Service (anonymous)

# Orders Routes
/api/v1/orders/*            → Orders Service (authenticated)
/api/v1/admin/orders/*      → Orders Service (admin)
```

---

## Frontend

> **Status: In development**

The frontend is a **Single Page Application (SPA)** that communicates **exclusively** with the API Gateway. It never connects directly to the microservices.

### Backend Connection

```
┌──────────────────────┐
│     Frontend SPA     │
│       React 19       │
└──────────┬───────────┘
           │
           │  HTTP Requests (Axios)
           │  Authorization: Bearer <JWT>
           │
           ▼
┌──────────────────────┐
│     API Gateway      │
│   localhost:5000     │
│                      │
│  • Validates JWT     │
│  • Rate Limiting     │
│  • Routes request    │
│  • CORS enabled      │
└──────────────────────┘
```

### Connection Configuration

The frontend automatically obtains the gateway URL from **.NET Aspire** via environment variables:

```typescript
// config.ts - The variable is injected by Aspire
const gatewayUrl = import.meta.env.VITE_API_GATEWAY_URL;
```

> **Note:** No manual `.env` file is required. Aspire automatically configures `VITE_API_GATEWAY_URL` when orchestrating the services.

### Communication Pattern

```typescript
// Axios base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY_URL,
  headers: { 'Content-Type': 'application/json' },
});

// JWT Interceptor
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### Frontend Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | React | 19.2.0 |
| Language | TypeScript | 5.9.3 |
| Build Tool | Vite (rolldown) | 7.2.5 |
| Routing | React Router | 7.10.1 |
| State/Cache | TanStack Query | 5.90.12 |
| HTTP Client | Axios | 1.13.2 |
| Styles | Tailwind CSS | 3.4.17 |

---

## Infrastructure

### Docker Compose (Manual Development)

```yaml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    volumes: [pgdata:/var/lib/postgresql/data]
    # Databases: identitydb, catalogdb, ordersdb

  redis:
    image: redis:7
    ports: ["6379:6379"]
    volumes: [redisdata:/data]
```

### .NET Aspire (Recommended)

Aspire automatically orchestrates all infrastructure:

```csharp
// AppHost.cs
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("Orderflow-postgres-data")
    .WithPgAdmin();

var identityDb = postgres.AddDatabase("identitydb");
var catalogDb = postgres.AddDatabase("catalogdb");
var ordersDb = postgres.AddDatabase("ordersdb");

var redis = builder.AddRedis("cache");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(port: 1080);  // UI for viewing emails
```

### Services Registered in Aspire

```csharp
// Each service with its dependencies
var identity = builder.AddProject<Projects.Orderflow_Identity>()
    .WithReference(identityDb)
    .WithReference(rabbitmq);

var catalog = builder.AddProject<Projects.Orderflow_Catalog>()
    .WithReference(catalogDb);

var orders = builder.AddProject<Projects.Orderflow_Orders>()
    .WithReference(ordersDb)
    .WithReference(rabbitmq)
    .WithReference(catalog);  // HTTP client

var notifications = builder.AddProject<Projects.Orderflow_Notifications>()
    .WithReference(rabbitmq)
    .WithReference(maildev);

var gateway = builder.AddProject<Projects.Orderflow_API_Gateway>()
    .WithReference(identity)
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(redis);
```

---

## Observability

The project implements **OpenTelemetry** for complete observability.

### Components

| Type | Technology | Description |
|------|------------|-------------|
| Traces | OpenTelemetry | Distributed request tracking |
| Metrics | OpenTelemetry | Counters, latencies, errors |
| Logs | Serilog | Structured logging with correlation |
| Dashboard | .NET Aspire | Integrated visualization |

### Aspire Dashboard

Available at: `https://localhost:17225`

Provides:
- Real-time status of all services
- Aggregated logs with search
- Distributed traces between services
- Performance metrics
- Health checks

### Health Checks

Each service exposes health endpoints:
```
GET /health       # General status
GET /alive        # Liveness probe
GET /ready        # Readiness probe
```

---

## Installation and Execution

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js 18+](https://nodejs.org/) (for the frontend)

### Option 1: With .NET Aspire (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd Orderflow_Project_2025

# Run with Aspire
dotnet run --project Orderflow.AppHost
```

**Aspire Dashboard**: https://localhost:17225

### Option 2: Manual Execution

```bash
# 1. Start infrastructure
docker-compose up -d

# 2. Run each service (in separate terminals)
dotnet run --project Orderflow.Identity
dotnet run --project Orderflow.Catalog
dotnet run --project Orderflow.Orders
dotnet run --project Orderflow.Notifications
dotnet run --project Orderflow.API.Gateway

# 3. Run frontend
cd Orderflow.Web
npm install
npm run dev
```

### Access URLs

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5000 |
| Frontend | http://localhost:5173 |
| Aspire Dashboard | https://localhost:17225 |
| MailDev (emails) | http://localhost:1080 |
| RabbitMQ Management | http://localhost:15672 |

### API Documentation (Scalar)

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5000/scalar/v1 |
| Identity | http://localhost:5001/scalar/v1 |
| Catalog | http://localhost:5002/scalar/v1 |
| Orders | http://localhost:5003/scalar/v1 |

---

## Testing

### Unit Tests

```bash
# Run all tests
dotnet test

# With code coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test Orderflow.Api.Identity.Test
```

### Test Projects

| Project | Coverage |
|---------|----------|
| `Orderflow.Api.Identity.Test` | AuthService, UserService, RoleService |
| `TestOrderflow.Console` | Integration tests |

### Testing Stack

| Component | Technology |
|-----------|------------|
| Framework | NUnit 4.2.2 |
| Mocking | Moq 4.20.72 |
| LINQ Mocking | MockQueryable 10.0.1 |

---

## Technology Stack

### Backend

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | .NET | 10.0 |
| Web Framework | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 10.0 |
| Database | PostgreSQL | 16 |
| Message Broker | RabbitMQ | Latest |
| Messaging | MassTransit | 8.4.0 |
| Cache | Redis | 7 |
| API Gateway | YARP | 2.3.0 |
| Rate Limiting | RedisRateLimiting | 1.2.0 |
| Authentication | JWT Bearer + Identity | 10.0 |
| Validation | FluentValidation | 12.1.0 |
| Email | MailKit | 4.12.1 |
| API Docs | Scalar / OpenAPI | Latest |
| Observability | OpenTelemetry | 1.14.0 |
| Orchestration | .NET Aspire | 13.0.0 |

### JWT Configuration

```bash
Jwt__Secret=<secret-key-minimum-32-characters>
Jwt__Issuer=Orderflow.Identity
Jwt__Audience=Orderflow.Api
Jwt__ExpiryInMinutes=60
```

### Development Credentials

| Field | Value |
|-------|-------|
| Email | admin@admin.com |
| Password | Test12345. |
| Role | Admin |

---

## Project Status

### Backend - Completed

- [x] Identity Microservice (JWT Authentication, Roles, User CRUD)
- [x] Catalog Microservice (Products, Categories, Stock)
- [x] Orders Microservice (Orders, States, Cancellation)
- [x] Notifications Worker (RabbitMQ, Emails)
- [x] API Gateway (YARP, Rate Limiting, JWT)
- [x] RabbitMQ/MassTransit Integration
- [x] PostgreSQL with migrations
- [x] OpenTelemetry
- [x] Unit tests

### Frontend - In Development

The frontend is in active development. It connects to the API Gateway to consume the backend services.

---

## License

<<<<<<< HEAD
## Estructura del Repositorio

 

```

Orderflow_project/

├── Orderflow.Identity/          # Microservicio de autenticación

├── Orderflow.Catalog/           # Microservicio de catálogo

├── Orderflow.Orders/            # Microservicio de pedidos

├── Orderflow.Notifications/     # Worker de notificaciones

├── Orderflow.API.Gateway/       # API Gateway (YARP)

├── Orderflow.Web/               # Frontend React

├── Orderflow.AppHost/           # Orquestación con Aspire

├── Orderflow.ServiceDefaults/   # Configuración compartida

├── Orderflow.Shared/             # DTOs, eventos y extensiones

├── Orderflow.Api.Identity.Test/ # Tests del servicio Identity

├── TestOrderflow.Console/       # Tests de integración

├── docker-compose.yaml          # Infraestructura (PostgreSQL, Redis)

├── ProyectoOrderflow.sln        # Solución de .NET

└── README.md                    # Este archivo

```

 
=======
This project is under the MIT license.
>>>>>>> origin/main

---

## Contributing

1. Fork the repository
2. Create a branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -m 'feat: new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Open a Pull Request
