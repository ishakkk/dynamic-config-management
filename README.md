# DynamicConfig

Dynamic configuration management library and API for .NET 8 — no restart required.

## Features

| Feature | Status |
|---|---|
| Version-based incremental refresh | ✅ |
| Serilog structured logging | ✅ |
| Health Checks (SQL, Redis, RabbitMQ) | ✅ |
| JWT Authentication & Authorization | ✅ |
| Redis Distributed Cache | ✅ |
| Polly Retry (exponential + jitter) | ✅ |
| Dead Letter Queue (RabbitMQ DLQ) | ✅ |
| Integration Tests (xUnit + Testcontainers) | ✅ |
| Unit Tests with Moq | ✅ |
| GitHub Actions CI | ✅ |
| EF Core (SQL Server + Migrations) | ✅ |
| Docker Compose (full ecosystem) | ✅ |
| Message Broker (RabbitMQ) | ✅ |

## Quick Start

```bash
docker compose up --build
```

Services:
- **API**: http://localhost:8080  
- **Swagger UI**: http://localhost:8080/swagger  
- **Health Check**: http://localhost:8080/health  
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)  
- **Sample Service A**: http://localhost:8081  

## Authentication

All `/api/configurations` endpoints require a JWT bearer token.

```bash
# Get a token (admin can create/update/delete)
curl -X POST http://localhost:8080/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"admin","secret":"admin123"}'

# Use the token
curl http://localhost:8080/api/configurations \
  -H "Authorization: Bearer <token>"
```

Demo credentials:
| ClientId | Secret | Role |
|---|---|---|
| `admin` | `admin123` | Admin (read + write) |
| `reader` | `reader123` | Reader (read only) |

## Version-Based Refresh

Every configuration entry has a `Version` counter (bigint). On each timer tick the library:
1. Queries only `MAX(Version)` from the database (one lightweight query)
2. If the DB max > local max → performs a full reload
3. Otherwise → skips the reload entirely

This drastically reduces unnecessary database round-trips in large deployments.

## Library Usage

```csharp
var reader = new ConfigurationReader(
    applicationName: "SERVICE-A",
    connectionString: "Server=...;Database=DynamicConfigDb;...",
    refreshTimerIntervalInMs: 5000);

string siteName  = reader.GetValue<string>("SiteName");
bool   basket    = reader.GetValue<bool>("IsBasketEnabled");
int    maxItems  = reader.GetValue<int>("MaxItemCount");
```

Supported types: `string`, `bool`, `int`, `double`.

## Project Structure

```
src/
  DynamicConfig.Domain/          # Entities, DTOs, Enums, Interfaces
  DynamicConfig.Infrastructure/  # EF Core, Redis, RabbitMQ, JWT, Health Checks
  DynamicConfig.Library/         # Standalone .NET library (ConfigurationReader)
  DynamicConfig.Api/             # ASP.NET Core Web API + Swagger + Serilog
  SampleService.A/               # Sample consumer using ConfigurationReader
tests/
  DynamicConfig.UnitTests/       # xUnit + Moq unit tests
  DynamicConfig.IntegrationTests/ # xUnit + WebApplicationFactory integration tests
docs/
  API.md, ARCHITECTURE.md, SETUP.md
```

## Running Tests

```bash
dotnet test DynamicConfig.sln
```

## Configuration

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | — | SQL Server connection string |
| `ConnectionStrings:Redis` | `localhost:6379` | Redis connection string |
| `RabbitMq:HostName` | `localhost` | RabbitMQ host |
| `Jwt:Secret` | — | JWT signing key (≥32 chars) |
| `Jwt:ExpirationMinutes` | `60` | Token lifetime |

"# dynamic-config-management" 
