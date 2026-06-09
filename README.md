# Betting Service

A simplified betting service API with a web UI for placing bets, managing markets, and settling events.

## Prerequisites

**Option A: .NET SDK**
- .NET 8 SDK or later

**Option B: Docker**
- Docker & Docker Compose

## Quick Start

### Using .NET SDK

```bash
cd src/BettingService
dotnet run
```

### Using Docker

```bash
docker-compose up
```

The service will be available at:
- **Web UI:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **API:** http://localhost:5000/api/

## Running Tests

```bash
# Install Playwright browsers (first time only)
cd tests/BettingService.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install

# Run tests (service must be running)
dotnet test
```

## API Overview

| Endpoint | Description |
|----------|-------------|
| `POST /api/users` | Create a user with starting balance |
| `GET /api/users/{id}/balance` | Get current balance |
| `POST /api/users/{id}/bets` | Place a bet |
| `GET /api/users/{id}/bets` | List user's bets |
| `POST /api/markets` | Create a market with selections |
| `GET /api/markets/{id}` | Get market state |
| `POST /api/markets/{id}/suspend` | Suspend a market |
| `POST /api/markets/{id}/resume` | Resume a market |
| `POST /api/events/{id}/result` | Post event result (triggers settlement) |
| `GET /api/events/{id}` | Get event details |
| `POST /api/admin/reset` | Reset all data |

Full API documentation available at `/swagger` when the service is running.
