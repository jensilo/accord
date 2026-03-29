# Agent Session History

This file is a chronological log of agent sessions working on Accord. Each session records what was done, key decisions made, and what comes next. When starting a new session, read the most recent entry here before doing anything else.

---

## Session 1 — 2026-03-23

**Context:** Initial planning session. No code written yet.

**What was done:**

- Analyzed the existing HARMONY codebase in full (Go, ~63 files, PostgreSQL, HTMX, OAuth2, per-user template management, PARIS parser, EIFFEL elicitation interface)
- Established rationale for the rewrite: simplify, improve maintainability, align with OOP mental models, use C#/.NET/Blazor
- Decided on tech stack: .NET 10, Blazor Server, EF Core + Npgsql, MailKit, Bootstrap 5
- Created project scaffold documentation: README.md, ARCHITECTURE.md, PLAN.md, CLAUDE.md, AGENTS_HISTORY.md (this file)

**Key decisions:**

- **Blazor Server** chosen over WASM — single project, no API layer needed, simpler for student contributors. `.NET 8 Auto` render mode noted as a clean future upgrade path.
- **Magic Link auth** replaces OAuth2 — no external provider dependencies, email is sufficient for access control
- **No per-user template management** — default PARIS templates are seeded at startup and served read-only to all users; this removes the most complex feature
- **Minimal user model** — only `Email` and `Id` stored; no profile editing; if the email is in the database, the user can log in
- **Requirements stay local** — `localStorage` JS interop, same as HARMONY, requirements never reach the server
- **Project name: Accord** — musical (harmony → accord) and semantic (agreement, fitting for requirements)
- **No profile management** — cut entirely; email as access control is sufficient

**What comes next (Phase 1):**

- Scaffold `Accord.Web` (.NET 10 Blazor Server project)
- Set up EF Core + Npgsql, define `User` and `MagicLinkToken` entities
- Implement `AuthService` and `MailService`
- Wire up ASP.NET Core cookie auth
- Build `Login.razor`, `Verify.razor`, `MainLayout.razor`, `Home.razor`
- Docker setup

---

## Session 2 — 2026-03-23

**Context:** Follow-up to Session 1, still planning — no code written yet.

**What was done:**

- Updated all planning documents (ARCHITECTURE.md, PLAN.md, CLAUDE.md) based on additional decisions

**Key decisions:**

- **Self-contained directory**: `accord/` will be moved to its own repo; no references to the parent HARMONY project
- **Requirements storage**: `localStorage` dropped in favour of simple DB storage (EF Core, flat list per user, no project grouping). More idiomatic in .NET. No storage at all is also acceptable for a first pass — don't over-engineer it.
- **Interactive forms are the critical feature**: The dynamic elicitation form (rendering fields from PARIS rule definitions, real-time parsing, variant switching) is explicitly called out as non-negotiable. Everything else is secondary.
- **Blazor Server confirmed**: WebSocket concern addressed — for academic/stable-network use, it's a non-issue. `.NET 8 Auto` render mode noted as future upgrade path if needed.

**What comes next (Phase 1 — unchanged):**

- Scaffold `Accord.Web` (.NET 10 Blazor Server)
- EF Core + Npgsql, `User` + `MagicLinkToken` entities
- Magic Link auth (`AuthService`, `MailService`, cookie auth)
- `Login.razor`, `Verify.razor`, `MainLayout.razor`, `Home.razor`
- Docker setup

---

## Session 3 — 2026-03-24

**Context:** Phase 1 implementation — all source files created for the first time.

**What was done:**

- Created full directory structure under `src/Accord.Web/` and `docker/`
- Wrote all 22 Phase 1 files exactly as specified:
  - `Accord.Web.csproj` (net10.0, Npgsql EF Core, MailKit)
  - `appsettings.json` and `appsettings.Development.json`
  - `Domain/Entities/User.cs` and `MagicLinkToken.cs`
  - `Infrastructure/Data/AppDbContext.cs` with EF Core model configuration
  - `Services/IAuthService.cs`, `AuthService.cs`, `IMailService.cs`, `MailService.cs`
  - `Components/_Imports.razor`, `App.razor`, `Routes.razor`
  - `Components/Layout/MainLayout.razor`, `NavMenu.razor`
  - `Components/Pages/Home.razor`, `Components/Pages/Auth/Login.razor`
  - `wwwroot/app.css`
  - `Program.cs` with full DI wiring, auth endpoints, and startup migration
  - `docker/Dockerfile`, `docker/docker-compose.yml`, `docker/docker-compose.dev.yml`
- Copied PARIS template JSON files from `../docs/templates/paris/` to `docs/templates/paris/` (11 files under v0.6.2/)

**Key decisions:**

- No deviations from the spec; files written exactly as provided
- No build was attempted (as instructed)

**What comes next (Phase 2):**

- Run `dotnet ef migrations add InitialCreate` to create the first EF Core migration
- Verify the app builds and runs against a local Postgres instance (via `docker/docker-compose.dev.yml`)
- Implement the EIFFEL elicitation feature: template loading service, PARIS parser, `Elicitation.razor`

---

## Session 4 — 2026-03-24

**Context:** Phase 1 completion — build fixes, migration, tooling.

**What was done:**

- Fixed two build errors from Session 3:
  1. `InteractiveServer` not in scope in `App.razor` — added `@using static Microsoft.AspNetCore.Components.Web.RenderMode` to `_Imports.razor`
  2. `SignInAsync`/`SignOutAsync` not found — added `using Microsoft.AspNetCore.Authentication` to `Program.cs`
- Upgraded MailKit to 4.15.1 (latest) to resolve MimeKit vulnerability advisory
- Confirmed clean build: 0 errors, 0 warnings
- Created initial EF Core migration (`20260324205927_Initial`) using `dotnet-ef` global tool
- Added `dotnet-tools.json` manifest at repo root with `dotnet-ef 10.0.5` as a local tool so future agents can run `dotnet tool restore` instead of relying on a global install

**Key decisions:**

- `/auth/verify` token validation is a minimal API endpoint (GET), not a Blazor component — this avoids the HttpContext limitations of interactive Blazor Server
- `/auth/logout` is also a GET endpoint + `NavigationManager.NavigateTo(..., forceLoad: true)` in NavMenu — simple and sufficient for this tool
- Bootstrap 5 loaded from CDN in `App.razor` — avoids needing libman/npm for development

**What comes next (Phase 2):**

- Define `TemplateSet` and `Template` entities + EF migration
- Implement `TemplateSeeder` — loads PARIS JSON from `docs/templates/paris/v0.6.2/` on startup
- Port `TemplateConfig` / `BasicTemplate` C# model from Go's `eiffel.BasicTemplate`
- Implement `ITemplateService` / `TemplateService`
- Build template list + detail pages (read-only)

---

## Session 5 — 2026-03-24

**Context:** Phase 2 implementation — templates.

**What was done:**

- Created `Domain/Entities/TemplateSet.cs` and `Template.cs`
- Created `Domain/Parser/TemplateConfig.cs` — deserializes PARIS JSON into `TemplateConfig`, `RuleDefinition`, `VariantDefinition`, `RuleExtra`; `RuleDefinition.GetValues()` normalises `value` field (handles string or array)
- Updated `AppDbContext` — added `TemplateSets` and `Templates` DbSets; added EF model configuration with unique indexes on `(Name, Version)` and `(TemplateSetId, Type, Name, Version)`
- Created `Infrastructure/Seeding/TemplateSeeder.cs` (includes `TemplateOptions`) — idempotent: scans version subdirectories under `Templates:DefinitionsPath`, groups by `type`, creates TemplateSets from the type name + version dir, inserts missing Templates
- Created `Services/ITemplateService.cs` and `TemplateService.cs`
- Updated `Program.cs` — registered `TemplateOptions`, `ITemplateService`, `TemplateSeeder`; seeder runs after migration at startup
- Created `Components/Pages/Templates/TemplateList.razor` and `TemplateDetail.razor`
- Added `Templates` link to `NavMenu.razor`
- Added `@using Accord.Web.Domain.Parser` to `_Imports.razor`
- Created EF migration `20260324210648_AddTemplates`
- Confirmed clean build: 0 errors, 0 warnings

**Key decisions:**

- TemplateSet name derived from JSON `type` field (uppercased, e.g. `"ebt"` → `"EBT"`); version from directory name (strip leading `v`)
- `RuleDefinition.Value` stored as `JsonElement?` to handle polymorphic string/array JSON value cleanly
- `TemplateSeeder` is a scoped service called explicitly in `Program.cs` after migration (same pattern as migration call), not a hosted service
- Template pages require `[Authorize]`

**What comes next (Phase 3):**

- Port `ParisParser` from Go to C# (`Domain/Parser/ParisParser.cs`, `ParsingResult.cs`)
- Build `Elicitation.razor` — the core product feature (template/variant picker, dynamic form, real-time parsing, save to DB)
- Add `Requirement` entity, `IRequirementService` / `RequirementService`, EF migration

---

## Session 6 — 2026-03-28

**Context:** Phase 3 — backend pieces of the EIFFEL elicitation feature (parser + requirement storage).

**What was done:**

- Created `Domain/Parser/ParsingResult.cs` — `ParsingLogLevel` enum, `ParsingLog` record, `ParsingResult` class with `Ok()` / `Flawless()` helpers
- Created `Domain/Parser/ParisParser.cs` — static `ParisParser.Parse(config, variantKey, segments)` method ported from Go's `BasicTemplate.Parse` + individual rule parsers; static `DisplayType.For(rule)` helper
- Created `Domain/Entities/Requirement.cs` — minimal entity with `Id`, `UserId`, `TemplateId`, `Content`, `CreatedAt`, navigation properties
- Created `Services/IRequirementService.cs` and `Services/RequirementService.cs` — scoped EF Core implementation (`SaveAsync`, `GetByUserAsync` ordered descending, includes Template)
- Updated `Infrastructure/Data/AppDbContext.cs` — added `DbSet<Requirement>`, EF config: HasKey, HasMaxLength(10000), FK UserId→Restrict, FK TemplateId→Cascade
- Updated `Program.cs` — registered `IRequirementService` / `RequirementService`
- Ran `dotnet ef migrations add AddRequirements` — migration created successfully
- Confirmed clean build: 0 errors, 0 warnings

**Key decisions:**

- `RuleExtra.After` defaults to `""` in `ParisParser` (not `" "`; `RuleExtra.Before` defaults to `" "`), matching the Go implementation's `buildRequirementIncrementally` logic
- `DisplayType.For` uses `"text"` / `"select"` / `"textarea"` string constants (not enum) since these are directly consumed by Razor templates
- `Elicitation.razor` not implemented — assigned to separate agent

**What comes next (Phase 3 — remaining):**

- Implement `Components/Pages/Eiffel/Elicitation.razor` — template/variant picker, dynamic form rendering per `DisplayType.For`, real-time `ParisParser.Parse` on input change, save button calling `IRequirementService.SaveAsync`

---

## Session 7 — 2026-03-28

**Context:** Phase 3 completion — implemented `Elicitation.razor`.

**What was done:**

- Created `src/Accord.Web/Components/Pages/Eiffel/Elicitation.razor` (and the `Eiffel/` directory)
- Two-column Bootstrap 5 layout: left column (template search + selection list, variant radio buttons, dynamic form fields), right column (requirement preview, log messages, save button)
- Template list loaded in `OnInitializedAsync` via `ITemplateService.GetTemplateSets()`; filtered in real time by a search box
- Template selection calls `TemplateService.ParseConfig(template)` to deserialize `TemplateConfig` from JSON; defaults to the first variant
- Dynamic form renders one field per rule in the selected variant using `DisplayType.For(rule)` — `text` → `<input>`, `select` → `<select>`, `textarea` → `<textarea>`; field width from rule `Size`
- Every input change invokes `RunParser()` which calls `ParisParser.Parse(config, variantKey, fieldValues)` and stores the result in `_parsingResult`
- Right column shows `_parsingResult.Requirement` in a `<pre>`, errors in `text-danger`, warnings in `text-warning`, notices in `text-muted`
- Save button disabled when no requirement text or when `_parsingResult.Ok()` is false; calls `IRequirementService.SaveAsync`; shows a "Requirement saved." message for 3 seconds
- Fixed Razor build error: empty string literals inside attribute expressions must use `string.Empty` not `""`
- Confirmed clean build: 0 errors, 0 warnings

**Key decisions:**

- Used `TemplateService.ParseConfig` instead of inline `JsonSerializer.Deserialize` — keeps deserialization in one place
- `string.Empty` instead of `""` in Razor attribute lambdas — Razor parser terminates the attribute on the inner quote otherwise

**What comes next:**

- Phase 3 is complete. Consider smoke-testing against a live Postgres instance via `docker-compose.dev.yml`
- Optional: add a "My Requirements" page listing saved requirements via `IRequirementService.GetByUserAsync`

---

## Session 8 — 2026-03-28

**Context:** Added xUnit test project covering the PARIS parser and key services.

**What was done:**

- Created `tests/Accord.Web.Tests/Accord.Web.Tests.csproj` — xUnit 2.9.3, FluentAssertions 7.0.0, Microsoft.EntityFrameworkCore.InMemory 10.0.0, project reference to `Accord.Web`
- Added the test project to `accord.sln` under a `tests` solution folder
- Created `tests/Accord.Web.Tests/GlobalUsings.cs` — `global using Xunit;`
- Created `tests/Accord.Web.Tests/Parser/ParisParserTests.cs` — 23 tests covering:
  - Equals rule (matching, case-insensitive, wrong value)
  - EqualsAny rule (all valid values including casing variants, no-match error)
  - Placeholder rule (accepts any input)
  - Missing required field → Error
  - Missing optional without `IgnoreMissingWhenOptional` → Notice with Downgrade
  - Missing optional with `IgnoreMissingWhenOptional` → no notice
  - Optional rule with wrong value → downgraded to Notice
  - Requirement string assembly with default and custom `Extra.Before`/`Extra.After`
  - Invalid variant throws `InvalidOperationException`
  - `Ok()` and `Flawless()` semantics
  - Multiple simultaneous errors
  - Full ESFA integration test (loads real esfa.json, verifies a complete requirement and a modality error)
- Created `tests/Accord.Web.Tests/Services/RequirementServiceTests.cs` — 4 tests with InMemory EF Core
- Created `tests/Accord.Web.Tests/Services/AuthServiceTests.cs` — 11 tests; fake `IMailService` inline using `file` scoped class

**Key decisions:**

- No Moq — `FakeMailService` is a `file`-scoped class implementing `IMailService` with no-op methods
- EF InMemory doesn't enforce FK constraints but `Include` does require the related entity to exist for navigation to work; fixed by seeding a `TemplateSet`+`Template` in `RequirementServiceTests`
- ESFA integration test reads the real template JSON file from a relative path anchored at `AppContext.BaseDirectory`
- All 37 tests pass

**What comes next:**

- Phase 3 is complete. The codebase is working end-to-end with tests.
- Consider smoke-testing against a live Postgres instance via `docker-compose.dev.yml`

---

## Session 9 — 2026-03-28

**Context:** Orchestration session — coordinated Phase 3 completion and added housekeeping.

**What was done:**

- Added `.gitignore` at repo root covering: macOS (`.DS_Store`), IDE (`.idea/`, `.vscode/`), .NET build outputs (`bin/`, `obj/`), NuGet artifacts, local secrets, test results, `.claude/` workspace files
- Launched sub-agents that implemented Phase 3 in full (sessions 6–8 above): parser, services, entity, migration, `Elicitation.razor`, and the xUnit test project
- Verified: `dotnet build` → 0 errors, 0 warnings; `dotnet test` → 37/37 tests pass

Phase 3 is complete.

**What comes next:**

- Phase 4: Localisation (`.resx` files for `en`/`de`, `IStringLocalizer`, `Accept-Language` detection, error pages, production Docker image)
- Optional: "My Requirements" page (`/requirements`, calls `IRequirementService.GetByUserAsync`)
- Smoke-test against live Postgres via `docker-compose.dev.yml`

---

## Session 10 — 2026-03-28

**Context:** V1 polish — missing error page, improved 404.

**What was done:**

- Created `Components/Pages/Error.razor` — minimal `/error` route (referenced by `UseExceptionHandler` in `Program.cs` for non-dev); previously missing, would have caused blank responses on unhandled exceptions
- Updated `Components/Routes.razor` — replaced boring 404 `<NotFound>` block with a monospace robot face + "Requirement #404: This page. Status: not found." dry humour copy
- Build confirmed: 0 errors, 0 warnings

**What comes next:**

- V1 is feature-complete. Remaining work is deployment: Bicep definitions for Azure Container Apps, GitHub Actions pipeline, GHCR or ACR registry choice.
- Optional later: "My Requirements" page, localisation (Phase 4).

---

## Session 11 — 2026-03-28

**Context:** Bug fix — 403 on restart after login.

**What was done:**

- Added `AddDataProtection().PersistKeysToFileSystem(...)` to `Program.cs` — keys are persisted to `<ContentRoot>/.keys/` (overridable via `DataProtection:KeyPath` in config)
- Added `using Microsoft.AspNetCore.DataProtection;` (extension method not in implicit using set)
- Added `.keys/` to `.gitignore`
- Build confirmed: 0 errors, 0 warnings

**Root cause:** Data Protection keys were generated in-memory. After an app restart, the old auth cookie (encrypted with the previous key set) could not be decrypted, resulting in a 403 instead of a redirect to login.

**What comes next:**

- For Docker/production, either mount a volume to the `.keys/` path or switch to `PersistKeysToDbContext` to store keys in Postgres (requires a new EF migration)

---

## Session 12 — 2026-03-28

**Context:** Improved Data Protection key storage — switched from file system to database.

**What was done:**

- Added `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.5 package
- `AppDbContext` now implements `IDataProtectionKeyContext` (adds `DataProtectionKeys` DbSet)
- `Program.cs` uses `PersistKeysToDbContext<AppDbContext>()` instead of file system
- Created EF migration `AddDataProtectionKeys`
- Removed `.keys/` from `.gitignore` (no longer needed)
- Build: 0 errors, 0 warnings

**Why:** File system keys don't survive Azure Container Apps deployments. DB storage works for local dev, Docker Compose, and ACA with Azure Database for PostgreSQL — all using the same existing Postgres instance, no extra cost.

**What comes next:**

- No further changes needed for V1. Ready for deployment setup (Bicep / GitHub Actions).

## Session 13 — 2026-03-28

**Context:** Root cause identified — 403 was not from ASP.NET Core at all.

**What was done:**

- Analysed DevTools Network trace provided by user: showed 14+ repeated `/_blazor/negotiate` POSTs after app restart, with the final response having `Server: AirTunes/935.7.1` and `HTTP/1.1 403 Forbidden`
- **Root cause:** macOS AirPlay Receiver (AirTunes) occupies port 5000 on macOS Monterey+. When Accord stops, AirPlay grabs port 5000. Browser reconnect logic keeps hitting port 5000 and receives AirPlay's 403 — nothing to do with ASP.NET Core auth or Data Protection
- Changed `launchSettings.json` `applicationUrl` from `http://localhost:5001` (was `5000`)
- Also applied three secondary hardening fixes made earlier in the session:
  - Added `opts.AccessDeniedPath = "/auth/login"` to cookie auth options (prevents bare 403 from `ForbidAsync`)
  - Set `IsPersistent = true` in `/auth/verify` `AuthenticationProperties` (persistent cookie survives browser restart)
  - Added `<div id="blazor-error-ui" style="display:none;">` to `App.razor` (Blazor reconnect UI was missing)

**Key decision:**

- Port 5001 is the permanent local dev port. User can optionally disable macOS AirPlay Receiver in System Settings → General → AirDrop & Handoff to reclaim port 5000, but this is not required.

**What comes next:**

- V1 is complete. Next session should proceed with deployment setup (Bicep / GitHub Actions / Azure Container Apps).

---

## Session 14 — 2026-03-29

**Context:** Code review and refactor before first commit.

**What was done:**

- **Critical bug fix:** `TemplateService.GetTemplateSets()` was not including related `Templates`. `Elicitation.razor` uses `_templateSets.SelectMany(s => s.Templates)` — without the include, all template sets returned an empty collection and the elicitation form never populated. Fixed by adding `.Include(s => s.Templates.OrderBy(t => t.Name))` to the query.
- **Simplified `TemplateList.razor`:** Was doing N+1 queries (one `GetTemplates(set.Id)` call per set in a loop). Now uses `set.Templates` from the eager-loaded collection; removed the `_templatesBySet` dictionary and the per-set async calls.
- **Fixed `Login.razor`:** Was using `[Inject]` attribute syntax inside `@code` — inconsistent with every other component in the project which uses `@inject` directives. Moved to `@inject`. Also added `private` access modifiers to all `@code` fields.
- **`MailService.cs` TLS support:** `SecureSocketOptions.None` was hardcoded, meaning no TLS — would fail against any production SMTP server. Added `UseTls` bool to `MailOptions` (default `false` for dev MailHog compatibility). When `true`, uses `SecureSocketOptions.Auto`.
- **`Elicitation.razor` disposal safety:** `Task.Delay(3000)` after saving a requirement had no cancellation — would continue running (and call `StateHasChanged`) on a disposed component if the user navigated away. Implemented `IDisposable` with a `CancellationTokenSource`; `Dispose()` cancels the delay.
- Build: 0 errors, 0 warnings. All 37 tests pass.

**Key decisions:**

- `GetTemplateSets()` now always includes templates — this is the only call site that needs them, and the added JOIN cost is trivial at this data scale.

**What comes next:**

1. **UI refinement** — The current elicitation UI needs work: it is functional again (bug fixed above) but the UX should be modernised. Priority: make the form feel polished and match the original HARMONY elicitation experience.
2. **Deployment pipeline:**
   - Azure Bicep files for Container Apps hosting the app + Azure Database for PostgreSQL
   - GitHub Actions workflow: build container → push to GitHub Container Registry (GHCR) → deploy to Azure Container Apps

---

## Session 15 — 2026-03-29

**Context:** UX modernisation of the EIFFEL elicitation interface. Bootstrap 5 enhanced approach (no framework swap).

**What was done:**

- **`ParisParser.cs` — `DisplayType.For()`**: Changed return values: `equals` → `"readonly"` (was `"text"`), `equalsAny` → `"datalist"` (was `"select"`). These are consumed only by `Elicitation.razor`.
- **`RequirementService.cs`**: Added `.Take(150)` to `GetByUserAsync` — limits history at the DB query level.
- **`wwwroot/eiffel.js`** (new): Keyboard shortcuts (Ctrl+F → open template picker modal; Alt+Enter → trigger check button; Alt+← / Alt+→ → previous/next variant), clipboard copy, Bootstrap tooltip init, modal show/hide helpers.
- **`App.razor`**: Added `<script src="eiffel.js">` reference after blazor.web.js.
- **`app.css`**: Added styles for `.eiffel-fixed` (readonly pre-filled fields), `.eiffel-info` (circular "i" tooltip trigger), `.eiffel-info-placeholder` (alignment spacer), `.history-item` (history panel buttons), and `kbd` sizing.
- **`Elicitation.razor`** — complete rewrite:
  - Template picker is now a Bootstrap modal (Ctrl+F or "Ändern" button), with a search box and a button-grid of templates — replaces the always-visible scrollable list.
  - `equals` fields are now rendered as gray readonly inputs (tabindex=-1, Tab skips them); pre-populated from `PrefillFixedValues()` on template/variant select.
  - `equalsAny` fields are now `<input>` + `<datalist>` instead of `<select>` — user can type freely or pick from the list.
  - Per-field "i" info icon (circular badge, Bootstrap tooltip) shown when `rule.Explanation` is set.
  - Collapsible accordion (Schablonenaufbau, Beispielsatz, Beschreibung) shown when data exists; Schablonenaufbau computed from variant rules (`<Name>` / `[Name]` / fixed value).
  - Variant keyboard hint (Alt+←/→) shown inline next to variant buttons.
  - Right-column history panel always rendered (not conditional); shows last 150 requirements, click-to-copy.
  - On successful check: auto-saves to DB, auto-copies to clipboard, clears form (fixed fields re-prefilled), refocuses first editable field, shows 3-second success banner.
  - Implements `IAsyncDisposable`; `eiffel.dispose()` called on component teardown.
  - `OnAfterRenderAsync` handles tooltip re-init and focus-first-input via flags (`_needsTooltipInit`, `_needsFocusFirstInput`).
- Build: 0 errors, 0 warnings. All 37 tests pass.

**Key decisions:**

- **Ctrl+F for template picker** (not Alt+F): Alt+F on German Mac layout produces "ƒ", which is not useful. Ctrl+F on macOS does not conflict with browser find (Cmd+F does). On Windows/Linux Ctrl+F would conflict — acceptable trade-off given academic/Mac-primary use context.
- **No "auto-store" toggle**: Always save to DB + auto-copy on successful check. Simpler than HARMONY's per-session option.
- **Bootstrap 5 enhanced** (no MudBlazor / Tailwind): The real work was functional improvements, not cosmetic. Zero new tooling needed.
- **`GetSchablonenaufbau()` called multiple times in template**: Avoids Razor parser issue with `@{ var x = ...; }` inside nested `@if` blocks.

**What comes next:**

- Login page visual polish (more appealing card-based layout, same Bootstrap 5).
- Consider sending a separate agent to redesign the magic link email template (plain-text or simple HTML).
- Phase 4: Localisation (`.resx`, `IStringLocalizer`, `en`/`de`).
- Deployment: Bicep / GitHub Actions / Azure Container Apps.

<!-- New sessions go above this line, most recent first is fine, or append — pick one convention and stick to it -->
