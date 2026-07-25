# SeatFlow API

SeatFlow is a REST API for event catalog management, seat reservation, and test payment processing.

The project demonstrates the development of a transactional booking system using ASP.NET Core, Entity Framework Core, PostgreSQL, JWT authentication, and Docker.

## Features

- User registration and JWT authentication
- Access and refresh tokens
- User and administrator roles
- Administrative management of:
  - venues;
  - halls;
  - seat layouts;
  - events;
  - event sessions
- Public event catalog
- Search, filters, sorting, and pagination
- Session seat maps with current availability
- Reservation of up to eight seats
- Ten-minute reservation hold
- Protection against double booking
- Optimistic concurrency through PostgreSQL `xmin`
- Reservation cancellation
- Test payment processing
- Automatic expiration of unpaid reservations
- PostgreSQL persistence and EF Core migrations
- Standardized Problem Details responses
- Liveness and readiness health checks
- Docker and Docker Compose support
- GitHub Actions CI
- Automated unit and integration tests

## Technology stack

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL
- Npgsql
- JWT Bearer authentication
- xUnit
- Docker
- Docker Compose
- GitHub Actions

## Architecture

The solution follows a layered architecture:

```text
SeatFlow.Api
    HTTP controllers, authentication middleware,
    background workers, and health checks

SeatFlow.Application
    Use-case contracts, query models, and validation

SeatFlow.Domain
    Entities, enums, business rules, and domain exceptions

SeatFlow.Infrastructure
    EF Core, PostgreSQL, authentication implementation,
    catalog queries, and reservation services
```

Project structure:

```text
src/
  SeatFlow.Api/
  SeatFlow.Application/
  SeatFlow.Domain/
  SeatFlow.Infrastructure/

tests/
  SeatFlow.UnitTests/
  SeatFlow.IntegrationTests/

scripts/
  smoke-test.ps1
```

## Main booking flow

1. A client opens the public event catalog.
2. The client selects an event session.
3. The API returns the current seat map.
4. An authenticated user selects one or more available seats.
5. SeatFlow creates a reservation and holds the seats for ten minutes.
6. The user completes a test payment.
7. The reservation becomes `Confirmed`.
8. Reserved seats become `Sold`.

If payment is not completed before expiration, the background worker marks the reservation as `Expired` and releases its seats.

Concurrent seat updates are protected by the PostgreSQL `xmin` concurrency token. When two requests attempt to reserve the same seat, only one request can succeed.

## Quick start with Docker

### Requirements

- Docker
- Docker Compose

Create a local environment file:

```powershell
Copy-Item .env.example .env
```

Replace the example passwords and JWT signing key in `.env`.

Start the API and PostgreSQL:

```powershell
docker compose up --build --detach
```

Check containers:

```powershell
docker compose ps
```

Run the smoke test:

```powershell
.\scripts\smoke-test.ps1
```

The API is available at:

```text
http://localhost:8080
```

Health checks:

```text
GET /health/live
GET /health/ready
```

OpenAPI document in the Development environment:

```text
http://localhost:8080/openapi/v1.json
```

Stop the project:

```powershell
docker compose down
```

Remove containers and the PostgreSQL volume:

```powershell
docker compose down --volumes
```

## Local development

Start PostgreSQL:

```powershell
docker compose up -d postgres
```

Configure the database connection through .NET User Secrets:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:Database" `
  "Host=127.0.0.1;Port=5433;Database=seatflow;Username=seatflow;Password=YOUR_PASSWORD" `
  --project .\src\SeatFlow.Api\SeatFlow.Api.csproj
```

Configure the JWT signing key:

```powershell
dotnet user-secrets set `
  "Jwt:SigningKey" `
  "YOUR_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARACTERS" `
  --project .\src\SeatFlow.Api\SeatFlow.Api.csproj
```

Run the API:

```powershell
dotnet run `
  --project .\src\SeatFlow.Api\SeatFlow.Api.csproj `
  --launch-profile http
```

The local API is available at:

```text
http://localhost:5080
```

Pending EF Core migrations are applied automatically during startup.

## API overview

### Authentication

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/revoke
GET  /api/auth/me
```

### Public event catalog

```text
GET /api/events
GET /api/events/{eventId}
GET /api/events/{eventId}/sessions
GET /api/sessions/{sessionId}/seats
```

Supported catalog parameters:

```text
search
category
venueId
startsFromUtc
startsToUtc
minPrice
maxPrice
sortBy
sortDirection
page
pageSize
```

### Reservations

Authentication is required.

```text
POST /api/reservations
GET  /api/reservations
GET  /api/reservations/{reservationId}
POST /api/reservations/{reservationId}/cancel
POST /api/reservations/{reservationId}/pay
```

Example reservation request:

```json
{
  "eventSessionId": "44444444-4444-4444-4444-444444444444",
  "sessionSeatIds": [
    "SESSION_SEAT_ID_1",
    "SESSION_SEAT_ID_2"
  ]
}
```

### Administrative API

The `Admin` role is required.

```text
POST   /api/admin/venues
GET    /api/admin/venues/{venueId}
PUT    /api/admin/venues/{venueId}
DELETE /api/admin/venues/{venueId}

POST   /api/admin/halls
GET    /api/admin/halls/{hallId}
PUT    /api/admin/halls/{hallId}
DELETE /api/admin/halls/{hallId}

POST   /api/admin/seats
GET    /api/admin/seats/{seatId}
PUT    /api/admin/seats/{seatId}
DELETE /api/admin/seats/{seatId}

POST   /api/admin/events
GET    /api/admin/events/{eventId}
PUT    /api/admin/events/{eventId}
DELETE /api/admin/events/{eventId}

POST   /api/admin/sessions
GET    /api/admin/sessions/{sessionId}
PUT    /api/admin/sessions/{sessionId}
POST   /api/admin/sessions/{sessionId}/cancel
DELETE /api/admin/sessions/{sessionId}
```

For local testing, a registered user can be assigned the administrator role directly in PostgreSQL:

```sql
UPDATE users
SET "Role" = 'Admin'
WHERE "Email" = 'user@seatflow.local';
```

The user must sign in again after changing the role because the role is stored inside the JWT access token.

## Error responses

The API uses ASP.NET Core Problem Details.

Common statuses:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

A `409 Conflict` is returned when:

- a selected seat is already reserved or sold;
- another request changes the seat concurrently;
- a resource cannot be removed because it is in use;
- a reservation can no longer be modified.

## Database

Main tables:

```text
users
refresh_tokens
venues
halls
seats
events
event_sessions
session_seats
reservations
reservation_seats
payments
```

The initial migration also creates demonstration catalog data:

- one venue;
- one hall;
- twelve seats;
- one event;
- one event session;
- twelve session seats.

## Build and tests

Restore dependencies:

```powershell
dotnet restore .\SeatFlow.sln
```

Build:

```powershell
dotnet build .\SeatFlow.sln `
  --configuration Release `
  --no-restore
```

Run all tests:

```powershell
dotnet test .\SeatFlow.sln `
  --configuration Release `
  --no-build
```

The project contains 64 automated tests covering:

- domain validation;
- authentication contracts;
- administrative authorization;
- event catalog validation;
- public routes;
- reservation transitions;
- payment transitions;
- solution structure.

## CI

GitHub Actions performs two jobs:

1. restores, builds, and tests the .NET solution;
2. builds the Docker image, starts PostgreSQL and the API, checks readiness, and runs the smoke test.

Workflow file:

```text
.github/workflows/ci.yml
```

## Security notes

- Secrets are not stored in the repository.
- `.env` is ignored by Git.
- Passwords are stored as secure hashes.
- Refresh tokens are stored as hashes.
- JWT access tokens have a limited lifetime.
- Administrative endpoints require the `Admin` role.
- Reservation ownership is checked for every user operation.
- Database writes use EF Core concurrency control.

## Possible future improvements

- Real payment provider integration
- Email notifications
- Distributed locking for multiple API instances
- Redis caching
- Outbox pattern and message broker
- Rate limiting
- Administrative frontend
- Mobile client