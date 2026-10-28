# PitchReserve

PitchReserve connects players with field owners. This backend uses .NET 8, EF Core, SQL Server, MediatR, FluentValidation, and Redis.

## Current features

- Player/owner registration, login, current-user details, and self-service name/phone editing.
- Owner field creation, listing, updating, and conditional deletion/deactivation.
- Cash bookings with a required Egyptian organizer mobile number, opening-hours/availability checks, and owner request acceptance followed by booking confirmation upon cash collection.
- Owner bookings for registered players, same-duration rescheduling, and cancellation at least 24 hours before kickoff.
- Player booking history, public matches, and authorized booking details.
- Join requests even when full, organizer decisions, withdrawal until kickoff, and organizer-only rejoining.
- Organizer editing of match visibility and total capacity before kickoff, including after organizer withdrawal.
- Reviews after a finished booking or accepted match, with rating recalculation.
- SQL Server transaction locks for conflicting writes; Redis caching and ASP.NET Core request rate limits.

Online checkout, payment-channel management, refunds, and frontend development are deferred. Existing cash collection endpoints remain.

## Start reading the code

Follow one feature at a time:

1. `BookingsController.Create` receives the request.
2. `CreateBookingCommand` carries the input.
3. `CreateBookingCommandValidator` checks input values.
4. `CreateBookingCommandHandler` checks identity, phone, schedule and availability, then creates the related records.
5. `Booking` and `MatchParticipant` contain the state changes and their rules.
6. `SqlServerTransactionCoordinator` makes competing field requests take turns and commits the related database changes together.

For a smaller example, read `UsersController.UpdatePhone` and the `UpdateCurrentUserPhoneNumber` feature first.

The Obsidian project has a detailed `Implementation Walkthrough` note explaining these classes and the configuration.

## Development configuration

Supply real SQL Server, Redis, and JWT settings before running the API. Existing appsettings values contain placeholders. Settings can be supplied through environment variables, for example `ConnectionStrings__DefaultConnection`, `Redis__ConnectionString`, and `JwtSettings__Secret`. Keep secrets out of source control.

This version serves Egypt: opening hours always use Cairo time, new payment amounts use EGP, and phone validation accepts Egyptian mobile numbers. Messages remain English. Booking timestamps are stored in UTC. Booking JSON times require an explicit offset, such as `2026-10-10T18:00:00+03:00` or a `Z` suffix.

The SQL lock wait is fixed at five seconds in `SqlServerTransactionCoordinator`. This is the time a request waits to obtain a database lock, not a lease expiration. The acquired lock lasts until the transaction commits or rolls back.

`RateLimiting` defaults to a 60-second window: login 10/IP, registration 5/IP, other reads 120/user-or-IP, other writes 30/user-or-IP. Exceeded limits return 429 with `Retry-After`. Limits are stored in the running API process and reset on restart; this simple version assumes one running API instance. Redis is used only for caching. Only the connection IP is trusted; this version does not enable forwarded-header processing.

Build the API and its production dependencies:

```powershell
dotnet build src/PitchReserve.Api/PitchReserve.Api.csproj
```

Run with configured services:

```powershell
dotnet run --project src/PitchReserve.Api
```

Swagger is available at `/swagger` in Development. No tests were added or executed during this implementation, as requested. Existing test projects were left untouched and were not included in the production-project build.

## Booking request example

`POST /api/bookings` requires a Player token. The organizer comes from the token, so omit `organizerPlayerId`.

```json
{
  "playgroundId": "replace-with-field-id",
  "startTime": "2026-10-10T18:00:00+03:00",
  "endTime": "2026-10-10T19:00:00+03:00",
  "paymentMethod": 1,
  "isOpenForPublicPlayers": true,
  "maxPlayersNeeded": 10
}
```

Enums use the existing numeric JSON format: `paymentMethod: 1` means CashOnArrival. Capacity 10 includes the organizer. A full match can receive pending requests; acceptance requires a free place.

If the current account has no valid phone, first send `PATCH /api/users/me/phone` with `{ "phoneNumber": "+201234567890" }`.

New API routes are listed in Swagger, including `/api/bookings/my`, `/api/bookings/public`, owner creation, schedule/cancel, join decisions, withdrawal/rejoin, owner fields, and field reviews. The rejoin route is organizer-only. Owners select an existing Player ID when creating bookings.

## Edit request examples

All edit routes require a login token. `PATCH /api/users/me` changes only the caller's profile:

```json
{
  "fullName": "Ahmed Hassan",
  "phoneNumber": "+201012345678"
}
```

Send either field or both. Omitted/null fields keep their existing values; `{}` or two nulls returns 400. Names must be nonblank and at most 100 characters; supplied phones must be Egyptian mobile numbers. A blank phone cannot remove the stored number. Email, password, role and business details are not editable here. `GET /api/auth/me` reads the updated profile; the existing phone-only route remains available.

The original organizer can send `PUT /api/bookings/{id}/match-settings`:

```json
{
  "isOpenForPublicPlayers": true,
  "maxPlayersNeeded": 10
}
```

The visibility field is required. Public capacity must be positive and cannot be lower than the current joined-player count (409). Capacity includes the organizer while joined. For a private booking, send `{ "isOpenForPublicPlayers": false }`; its public capacity becomes null. Accepted players and pending requests remain stored. Accepted players keep access and can withdraw before kickoff. Pending requests cannot be decided while private but can be decided after reopening. Editing is available before kickoff in PendingOwnerApproval, AcceptedAwaitingCash or Confirmed; cancelled, expired and started bookings return 409. Organizer withdrawal does not remove editing authority. The settings endpoint does not change the schedule, approval or payment.

Owners already edit their playground through `PUT /api/playgrounds/{id}`. This is a full update: submit every editable value, including unchanged ones. For example:

```json
{
  "name": "Cairo Football Field",
  "size": 0,
  "surfaceType": 1,
  "hourlyRate": 500,
  "locationCity": "Cairo",
  "address": "10 Example Street, Nasr City",
  "latitude": null,
  "longitude": null,
  "openHour": "08:00:00",
  "closeHour": "23:00:00",
  "status": 1
}
```

Here size 0 means FiveASide, surface 1 means ArtificialTurf, and status 1 means Opened. Read the owner's field details first through `GET /api/playgrounds/owner/{id}` and use its actual values. Another owner cannot edit the field; opening-hour changes cannot invalidate active future bookings, and rate changes do not reprice existing bookings. Success returns 204; the two new profile/settings endpoints return 200 with the existing `Result` shape.

These edits reuse existing columns and need no new database migration.

### Runtime checks when testing resumes

- Verify own-profile changes and rejection of blank/too-long names, bad phones and empty patches; omitted values must remain unchanged.
- Verify another owner cannot edit the playground and conflicting opening hours are rejected.
- Verify only the organizer can edit match settings, including after withdrawing.
- Race a capacity reduction with request acceptance or organizer rejoining; the joined count must never exceed public capacity.
- Make a public match private: check public discovery, preserved accepted players/pending requests, blocked requests/decisions and accepted-player withdrawal. Reopen with valid capacity and decide a preserved request.
- Reject settings changes on cancelled, expired or started bookings; retain cash acceptance followed by collection as separate steps.

Automated tests and database execution remain deferred. Build verification checks compilation; these runtime scenarios have not been executed.

## Database preparation — choose the correct path

Migration files and SQL scripts are prepared in `src/PitchReserve.Infrastructure/Persistence/Migrations`. They were generated without applying them to a database.

- **Fresh, empty database:** review and apply `InitialNonPaymentSchema.sql`, or apply the `InitialNonPaymentSchema` EF migration using your configured connection.
- **Existing database without EF migration history:** inspect the actual schema against the generated baseline, stop API writes, take a backup, and review `AdoptExistingDatabase.sql`. It adds feature columns/indexes, converts legacy extra-player capacity to total capacity, recounts accepted participants, moves legacy pending/unpaid cash bookings to PendingOwnerApproval, and records the baseline migration. It deliberately stops when incompatible schema/history or invalid capacity/organizer records are found.
- **Existing migration history or partially changed schema:** do not use either script blindly. Reconcile the actual schema with a separate reviewed migration.

Do not apply the create-all initial migration to a populated legacy database. The adoption script is a guarded template for a matching baseline, not a general schema repair tool. It does not remove booking or payment history. Migration rollback/data conversion has not been runtime-tested; restore a backup if an applied adoption must be reversed.

The design-time `PitchReserveDbContextFactory` helps EF commands construct the context during migration generation. The running API still obtains its context through dependency injection. The factory alone does not apply a migration.

## Privacy and behavior notes

Unrelated callers cannot read private bookings. Public match responses omit contact, payment and roster information. Accepted participants can see booking details and accepted roster; organizer contact/payment data is limited to the organizer and field owner.

Cash reservations start in PendingOwnerApproval and reserve the slot. Owners list waiting requests with GET /api/bookings/owner?status=3, then call POST /api/bookings/{id}/accept: Booking becomes AcceptedAwaitingCash and Payment stays Pending. Public discovery and participation become available after acceptance. Later, POST /api/payments/{paymentId}/confirm-cash records collection, changes Payment to Succeeded and Booking to Confirmed in one transaction. Collection before acceptance is rejected; collection at or after kickoff is allowed. Ended owner-accepted cash bookings can qualify for a review without payment success. Paid bookings cannot be cancelled while refunds are deferred. Appended numeric statuses are PendingOwnerApproval = 3 and AcceptedAwaitingCash = 4; existing status values keep their numbers.

Fields with future active bookings cannot be deleted. A field with any booking/payment history is kept as Inactive, with history and reviews preserved. A field with no history can be removed with its reviews.

Logging uses a small Serilog console/daily-file setup plus built-in request logging. The previous custom logging options, queue/drop monitoring, tracing middleware and timing pipeline have been removed for simplicity. The existing untracked logging tests are unchanged and refer to the previous implementation; they were not compiled or executed in this phase.
