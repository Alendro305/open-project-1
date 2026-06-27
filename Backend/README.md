# ChopChop.Api

ASP.NET Core (.NET 10) backend for ChopChop online functionality.
SQLite + EF Core + ASP.NET Identity + JWT bearer auth, exposed as minimal-API endpoints.

## Run

```bash
cd Backend/ChopChop.Api
dotnet run
# -> http://localhost:5080  (see Properties/launchSettings.json)
```

The database (`chopchop.db`) is created and migrated automatically on first start,
and the `Player`/`Admin` roles are seeded.

## Configuration

`appsettings.json` → `Jwt.Secret` **must be replaced** for any non-dev use. For local dev,
prefer user-secrets:

```bash
dotnet user-secrets set "Jwt:Secret" "<a long random key >= 32 chars>"
```

## Endpoints

| Method | Route                       | Auth | Body / Query                              | Returns          |
|--------|-----------------------------|------|-------------------------------------------|------------------|
| GET    | `/`                         | —    | —                                         | service status   |
| POST   | `/api/auth/register`        | —    | `{ email, displayName, password }`        | `AuthResponse`   |
| POST   | `/api/auth/login`           | —    | `{ email, password }`                     | `AuthResponse`   |
| GET    | `/api/auth/me`              | JWT  | —                                         | `UserDto`        |
| GET    | `/api/profile`              | JWT  | —                                         | `ProfileDto`     |
| PUT    | `/api/profile`              | JWT  | `{ displayName?, level?, experience?, coins?, totalPlayTimeSeconds? }` | `ProfileDto` |
| GET    | `/api/profile/save`         | JWT  | —                                         | `SaveDataDto`    |
| PUT    | `/api/profile/save`         | JWT  | `{ saveDataJson }`                        | `SaveDataDto`    |
| GET    | `/api/leaderboard`          | —    | `?category=default&top=20`                | `ScoreEntryDto[]`|
| POST   | `/api/leaderboard`          | JWT  | `{ category, score }`                     | `{ id }`         |

`AuthResponse = { accessToken, expiresUtc, user: { id, email, displayName } }`.
Send the token as `Authorization: Bearer <accessToken>`.

OpenAPI document served at `/openapi/v1.json` in Development.

## Project layout

```
ChopChop.Api/
  Program.cs            DI + pipeline wiring
  Domain/               EF entities (ApplicationUser, PlayerProfile, ScoreEntry)
  Data/                 AppDbContext, DbSeeder
  Contracts/            request/response DTOs (the wire contract shared with Unity)
  Services/             ITokenService / JwtTokenService, JwtOptions
  Endpoints/            minimal-API endpoint groups (Auth, Profile, Leaderboard)
  Migrations/           EF Core migrations
```

## Adding a migration

```bash
dotnet ef migrations add <Name> --project ChopChop.Api/ChopChop.Api.csproj
```
