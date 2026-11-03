# PitchReserve — Sports Pitch Reservation & Matchmaking API

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core Web API](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4?style=flat&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=flat&logo=nuget&logoColor=white)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-CC2927?style=flat&logo=microsoftsqlserver&logoColor=white)
![Redis](https://img.shields.io/badge/Cache-Redis-DC382D?style=flat&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Container-Docker%20Compose-2496ED?style=flat&logo=docker&logoColor=white)

**PitchReserve** is a concurrency-safe RESTful backend API built with **ASP.NET Core 8** that connects football players with pitch owners across Egypt. It manages end-to-end pitch discovery, slot scheduling, owner approval workflows, cash-on-arrival settlements, public match matchmaking, and verified post-match reviews.

---

## Architecture & Engineering Highlights

The solution follows **Clean Architecture** and **CQRS (MediatR)** across four decoupled projects:

```text
┌─────────────────────────────────────────────────────────────┐
│                      PitchReserve.Api                       │
│   Controllers • Rate Limiting • Exception Middleware • DI   │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
               ▼                               ▼
┌─────────────────────────────┐ ┌─────────────────────────────┐
│  PitchReserve.Application   │ │ PitchReserve.Infrastructure │
│  MediatR Commands & Queries │◄┤ EF Core • SQL App Locks     │
│  FluentValidation Pipelines │ │ Redis Cache • JWT / PBKDF2  │
└──────────────┬──────────────┘ └──────────────┬──────────────┘
               │                               │
               └───────────────┬───────────────┘
                               ▼
               ┌─────────────────────────────┐
               │     PitchReserve.Domain     │
               │ Entities • State Rules      │
               │ Domain Exceptions • Enums   │
               └─────────────────────────────┘
```

### Key Technical Decisions

1. **Concurrency-Safe Booking & Capacity Control**
   - **Pessimistic Serialization (`sp_getapplock`):** Competing booking creation, rescheduling, and capacity changes acquire transaction-scoped SQL Server application locks via `SqlServerTransactionCoordinator` (with a 5-second lock timeout and deterministic resource ordering to prevent deadlocks).
   - **Defense-in-Depth:** Combined with EF Core `rowversion` optimistic concurrency tokens and SQL Server `CHECK` constraints so concurrent requests never double-book a time slot or exceed match capacity.

2. **Cairo-Timezone Scheduling Engine**
   - All timestamps are persisted in **UTC** while enforcing **Cairo local time (`Africa/Cairo`)** for pitch operating hours.
   - Supports operating windows that cross midnight, whole-hour slot alignment, overlap detection against active reservations, and a strict 24-hour pre-kickoff cancellation policy.

3. **Two-Stage Cash Settlement & Matchmaking**
   - **Reservation State Machine:** Cash reservations start in `PendingOwnerApproval` (reserving the slot), transition to `AcceptedAwaitingCash` upon owner acceptance (unlocking public match discovery and participant join requests), and finalize as `Confirmed` when `POST /api/payments/{paymentId}/confirm-cash` records payment collection in an atomic transaction.
   - **Privacy-Preserving Projections:** Public match queries hide organizer phone numbers, payment statuses, and private rosters; full details are exposed only to accepted participants, the organizer, and the pitch owner.

4. **Caching, Rate Limiting & Security**
   - **Redis Caching:** Read-heavy playground and schedule queries are cached in Redis with prefix-based cache invalidation on writes.
   - **Partitioned Rate Limiting:** Built-in ASP.NET Core 60-second fixed-window rate limits partitioned by IP and authenticated user (`10 req/min` for login, `5 req/min` for registration, `120 req/min` for reads, `30 req/min` for writes), returning `429 Too Many Requests` with `Retry-After`.
   - **Authentication:** Stateless JWT Bearer authentication with PBKDF2 password hashing and Egyptian mobile number (`+20`) validation.

---

## Project Structure

```text
PitchReserve/
├── src/
│   ├── PitchReserve.Domain/          # Aggregates, Entities (Booking, Playground, MatchParticipant, Payment, Review), Domain Enums
│   ├── PitchReserve.Application/     # CQRS Commands/Queries, FluentValidation Validators, Pipeline Behaviors, Interfaces
│   ├── PitchReserve.Infrastructure/  # EF Core DbContext, SqlServerTransactionCoordinator, Migrations, Redis Cache, Auth
│   ├── PitchReserve.Api/             # REST Controllers (32 endpoints), Middleware, Rate Limiting, Dockerfile
│   └── docker-compose.yml            # Multi-container orchestration (API, SQL Server, Redis)
└── PitchReserve.slnx
```

---

## Getting Started

### Option 1: Run with Docker Compose

From the `src/` directory, start the API, SQL Server, and Redis containers:

```powershell
cd src
docker compose up --build
```

### Option 2: Run Locally with .NET 8 CLI

1. **Configure Environment Variables or User Secrets:**
   Provide connection strings and JWT settings via environment variables (or `dotnet user-secrets`):
   - `ConnectionStrings__DefaultConnection`
   - `Redis__ConnectionString`
   - `JwtSettings__Secret`

2. **Apply Database Migrations:**
   ```powershell
   dotnet ef database update --project src/PitchReserve.Infrastructure --startup-project src/PitchReserve.Api
   ```

3. **Build and Launch the API:**
   ```powershell
   dotnet build src/PitchReserve.Api/PitchReserve.Api.csproj
   dotnet run --project src/PitchReserve.Api
   ```
   Navigate to `/swagger` in `Development` to explore and test all 32 endpoints.

---

## Core API Workflows & Examples

### 1. Create a Booking (`POST /api/bookings`)
Requires a `Player` bearer token. Booking timestamps require an explicit timezone offset (e.g., `+03:00` or `Z`):

```json
{
  "playgroundId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startTime": "2026-10-10T18:00:00+03:00",
  "endTime": "2026-10-10T19:00:00+03:00",
  "paymentMethod": 1,
  "isOpenForPublicPlayers": true,
  "maxPlayersNeeded": 10
}
```
*Note: `paymentMethod: 1` corresponds to `CashOnArrival`. `maxPlayersNeeded` includes the organizer.*

### 2. Update Match Visibility & Capacity (`PUT /api/bookings/{id}/match-settings`)
Allows the match organizer to toggle public matchmaking or adjust total player capacity before kickoff:

```json
{
  "isOpenForPublicPlayers": true,
  "maxPlayersNeeded": 10
}
```
*Validation ensures capacity never drops below the currently accepted player count (`409 Conflict`).*

### 3. Update Playground Details (`PUT /api/playgrounds/{id}`)
Allows the pitch owner to update field configuration and operating hours:

```json
{
  "name": "Cairo Football Field",
  "size": 0,
  "surfaceType": 1,
  "hourlyRate": 500,
  "locationCity": "Cairo",
  "address": "10 Example Street, Nasr City",
  "latitude": 30.0561,
  "longitude": 31.3301,
  "openHour": "08:00:00",
  "closeHour": "23:00:00",
  "status": 1
}
```
*Opening-hour updates are validated within a transaction lock so active future bookings are never invalidated.*

### 4. Update User Profile (`PATCH /api/users/me`)
```json
{
  "fullName": "Ahmed Hassan",
  "phoneNumber": "+201012345678"
}
```

---

## License

This project is licensed under the [MIT License](LICENSE).
