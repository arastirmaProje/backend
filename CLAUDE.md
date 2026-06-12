# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations remove

# Docker
docker compose up -d --build
docker image prune -f
```

There are no automated tests in this project.

## Architecture

ASP.NET Core 8.0 Web API for a personnel management system. Stack: PostgreSQL + EF Core 8, JWT auth, BCrypt, MailKit, Swagger.

**Folder layout:**
- `Controllers/` — thin controllers; all protected with `[Authorize]`, delegate immediately to services
- `Services/<Domain>/` — every domain has `IXService` + `XService`; all methods return `Task<ServiceResponse<T>>`
- `Models/` — EF entities; `Data/AppDbContext.cs` is the single DbContext
- `Models/DTOs/` — request/response DTOs organized by domain
- `Helpers/ServiceResponse.cs` — universal response wrapper (`Success`, `Message`, `Data`, `Errors`)
- `Resources/SharedResource.resx` — all user-facing strings in Turkish via `IStringLocalizer<SharedResource>`
- `Migrations/` — 57+ EF migrations; auto-applied on startup
- `wwwroot/uploads/` — file uploads served as static files on port 8080

## Key Patterns

**ServiceResponse<T>** — every service method returns this. Use `ServiceResponse<T>.SuccessResult(data)` and `ServiceResponse<T>.ErrorResult(_localizer["Key"])`. Controllers map `result.Success` to `Ok` / `BadRequest`.

**User identity in controllers** — call `GetUserIdFromToken()` (defined in base controller) which parses the `"uid"`, `"id"`, or `ClaimTypes.NameIdentifier` claim. Returns `Guid.Empty` on failure.

**Active membership check** — before any business-scoped operation, check both `BusinessMembers` (for employees) and `Businesses.OwnerId` (for owners). Owners are not automatically in `BusinessMembers`. Failing to check both is a common bug source.

**Soft deletes** — entities use `IsActive` flag; never hard-delete. Filter by `.IsActive == true` in queries.

**Localization** — inject `IStringLocalizer<SharedResource>` as `_localizer`. All error/success messages use `_localizer["KeyName"]`. Add new keys to `Resources/SharedResource.resx`.

## Domain Overview

| Domain | Notable detail |
|--------|---------------|
| Auth | JWT (7-day), BCrypt passwords, 6-digit password reset codes (15 min) |
| Business | Has `OwnerId` + self-referential parent/child (`SubBusinesses`) |
| BusinessMember | Join table between User and Business; holds role, department, IsActive |
| Performance | Calls external AI API `https://personelim-ai-api.onrender.com/api/performans` via named `HttpClient("AiPerformance")` (60s timeout); stores result in `PerformanceReports` |
| Slack | `SlackWebhooks` table stores webhook URLs per business + event type; `SlackService` posts fire-and-forget |
| Shifts | Stores `TotalHours` as decimal; used for performance score calculation |
| Leaves | `MemberLeaves` linked to `BusinessMember`, not directly to `User` |

## Configuration

Environment variables override `appsettings.json`:
- `DATABASE_URL` — PostgreSQL connection string
- `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME`

Migrations run automatically on app startup via `context.Database.MigrateAsync()` in `Program.cs`.
