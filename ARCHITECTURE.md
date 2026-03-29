# Architecture

> This directory is self-contained and will be moved to its own repository. It has no runtime dependency on the surrounding HARMONY project. All referenced paths (e.g. `docs/templates/paris/`) are relative to this directory.

## Stack

| Layer | Technology | Notes |
|---|---|---|
| Runtime | .NET 10 | LTS path, modern C# features |
| Web framework | ASP.NET Core + Blazor Server | Interactive components, no separate API layer |
| ORM | Entity Framework Core 10 + Npgsql | Code-first migrations, PostgreSQL |
| Email | MailKit | Magic Link delivery |
| Styling | Bootstrap 5 | Consistent with HARMONY feel |
| Container | Docker + docker-compose | Dev and production images |

---

## Project structure

```
accord/
├── src/
│   └── Accord.Web/                        # Single deployable application
│       ├── Components/
│       │   ├── App.razor                  # Root component
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor       # App shell with nav
│       │   │   └── NavMenu.razor
│       │   └── Pages/
│       │       ├── Home.razor
│       │       ├── Auth/
│       │       │   ├── Login.razor        # Email input → send magic link
│       │       │   └── Verify.razor       # Token landing page → issue session
│       │       └── Eiffel/
│       │           └── Elicitation.razor  # Core elicitation interface
│       ├── Domain/
│       │   ├── Entities/
│       │   │   ├── User.cs
│       │   │   ├── MagicLinkToken.cs
│       │   │   ├── TemplateSet.cs
│       │   │   └── Template.cs
│       │   └── Parser/
│       │       ├── ParisParser.cs         # Core parsing logic (ported from Go)
│       │       ├── ParsingResult.cs
│       │       └── TemplateConfig.cs      # JSON deserialization of PARIS templates
│       ├── Infrastructure/
│       │   ├── Data/
│       │   │   ├── AppDbContext.cs
│       │   │   └── Migrations/
│       │   └── Seeding/
│       │       └── TemplateSeeder.cs      # Loads PARIS JSON on startup
│       ├── Services/
│       │   ├── IAuthService.cs / AuthService.cs
│       │   ├── IMailService.cs / MailService.cs
│       │   ├── ITemplateService.cs / TemplateService.cs
│       │   └── IRequirementService.cs / RequirementService.cs
│       ├── wwwroot/                        # Bootstrap, custom CSS, favicon
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
├── docs/
│   └── templates/paris/                   # PARIS JSON definitions (unchanged from HARMONY)
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── README.md
├── PLAN.md
├── ARCHITECTURE.md
├── CLAUDE.md
└── AGENTS_HISTORY.md
```

---

## Domain model

```csharp
// Minimal — email is the only identity we need
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

// One-time login token, short-lived
public class MagicLinkToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;  // cryptographically random
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public User User { get; set; } = default!;
}

// Seeded at startup, read-only for all users
public class TemplateSet
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ICollection<Template> Templates { get; set; } = [];
}

public class Template
{
    public Guid Id { get; set; }
    public Guid TemplateSetId { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Config { get; set; } = default!;  // Raw JSON, deserialized on demand
    public TemplateSet TemplateSet { get; set; } = default!;
}

// Scratch-pad — flat list per user, no project grouping
public class Requirement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public string Content { get; set; } = default!;  // The generated requirement text
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = default!;
    public Template Template { get; set; } = default!;
}
```

---

## Authentication flow (Magic Link)

```
1. User enters email on /auth/login
2. Server: look up or create User by email
3. Server: generate cryptographically random token, store MagicLinkToken (15-min TTL)
4. Server: send email with link → /auth/verify?token=<token>
5. User clicks link
6. Server: validate token (exists, not used, not expired)
7. Server: mark token as used, issue session cookie (ASP.NET Core cookie auth)
8. User is authenticated, redirected to home
```

Session management uses ASP.NET Core's built-in cookie authentication — no custom session table needed.

---

## Template management

Templates are seeded from `docs/templates/paris/` at application startup via `TemplateSeeder`. The seeder is idempotent — it checks existing records by `(Type, Name, Version)` and only inserts missing ones. Users cannot create, edit, or delete templates. This removes the most complex part of HARMONY.

---

## EIFFEL elicitation

The elicitation interface is a Blazor Server component. It:

1. Loads available template sets from `ITemplateService`
2. Renders a search/filter UI (templates list, variant picker)
3. Deserializes the selected template's `Config` JSON into `TemplateConfig`
4. **Renders input fields dynamically from `TemplateConfig.Rules`** — this is the critical feature. Each rule entry maps to a display type (text input, textarea, single select). Fields marked optional are shown/hidden accordingly. Variant switching replaces the active rule set entirely.
5. On each input change, calls `ParisParser.Parse()` to produce a `ParsingResult`
6. Displays the resulting requirement text + any errors/warnings/notices in real time
7. Optionally saves the finished requirement via `IRequirementService` — stored as a flat list per user in the database (scratch-pad, no project grouping)

---

## Configuration

`appsettings.json` covers all settings. Environment variable overrides use the standard .NET convention (`Section__Key`).

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=accord;Username=root;Password=root"
  },
  "Auth": {
    "CookieName": "accord.session",
    "MagicLinkTtlMinutes": 15
  },
  "Mail": {
    "Host": "localhost",
    "Port": 25,
    "From": "accord@example.com"
  },
  "Templates": {
    "DefinitionsPath": "docs/templates/paris"
  }
}
```

---

## Key design decisions

| Decision | Rationale |
|---|---|
| Blazor Server over WASM | Single project, no API layer, simpler for student contributors. WebSocket concern is negligible for the target (academic, stable networks). `.NET 8 Auto` mode is a clear upgrade path with minimal refactoring if needed. |
| No per-user template management | Removed entirely. Default PARIS templates serve all users. Cuts the most complex feature with zero loss of core value. |
| Magic Link over OAuth | Removes all external provider dependencies. An email is sufficient access control for this tool. |
| Email as sole identity | No profile management. If a user's email is in the database, they can log in. That is all. |
| Requirements stored in DB (scratch-pad) | Simpler and more idiomatic in .NET/EF Core than `localStorage` JS interop. Flat list per user — no project grouping. A future extension can add project-level organisation. No storage at all is also acceptable for a first pass. |
| Dynamic template forms are the core feature | Every other decision is secondary. The elicitation form must correctly render any PARIS template definition — arbitrary rule sets, display types, optional/required fields, variant switching. |
| EF Core code-first | Replaces raw SQL migrations with managed C# migrations. Easier to maintain and understand for students new to the codebase. |
| Seeded templates, not user-owned | Simplifies data model significantly. Templates are static infrastructure, not user data. |
