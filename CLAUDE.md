# Claude Agent Instructions — Accord

## What is this project?

Accord is a C# / .NET 10 / Blazor Server rewrite of HARMONY, a requirements elicitation tool for template-based natural language approaches (specifically the PARIS pattern language). Read [README.md](README.md) for the rationale.

This directory is self-contained. It will be moved to its own repository — do not introduce any dependency on the surrounding HARMONY project.

## Before you do anything

1. **Read [AGENTS_HISTORY.md](AGENTS_HISTORY.md)** — the most recent session entry tells you exactly where things were left off and what decisions have been made. Do not re-litigate settled decisions.
2. **Read [PLAN.md](PLAN.md)** — check which phase is in progress and which tasks remain.
3. **Read [ARCHITECTURE.md](ARCHITECTURE.md)** — understand the structure before touching code.

## After your session

Before finishing, **append a new entry to [AGENTS_HISTORY.md](AGENTS_HISTORY.md)** with:
- Date
- What you did (concrete, specific)
- Any key decisions made and why
- What the next agent should pick up

Keep it brief but complete enough that the next agent can start without context from this conversation.

---

## Project principles

- **KISS.** Do not add complexity that isn't asked for. Do not design for hypothetical future requirements.
- **No unnecessary abstractions.** Three similar lines are better than a premature helper. Don't create utilities for one-off operations.
- **No over-engineering.** Don't add error handling for scenarios that can't happen. Trust the framework.
- **Minimal user model.** The `User` entity is intentionally minimal — just `Id`, `Email`, `CreatedAt`. Do not expand it without a concrete requirement.
- **Templates are infrastructure, not user data.** `TemplateSet` and `Template` are seeded at startup and read-only at runtime. Do not add per-user template management.
- **Interactive forms are the core feature.** The dynamic elicitation form — rendering fields from template rule definitions, real-time parsing, variant switching — must work correctly and reliably. This is non-negotiable.
- **Requirements storage is a scratch-pad.** Requirements are saved per user in the database as a simple flat list — no project grouping, no versioning. If even that is too much, no storage at all is acceptable for an initial pass. Do not over-engineer this.

---

## Conventions

- **Language:** C# / .NET 10. Use modern C# features (primary constructors, collection expressions, etc.) where they improve clarity.
- **Project:** Single project `Accord.Web` — Blazor Server. No separate API project.
- **Dependency injection:** Use ASP.NET Core's built-in DI throughout. Register services in `Program.cs`.
- **Database:** EF Core code-first. All schema changes via migrations (`dotnet ef migrations add`). Never modify the database manually.
- **Authentication:** ASP.NET Core cookie auth + custom Magic Link flow (`AuthService`). Do not use ASP.NET Core Identity — it's too heavy for this use case.
- **Localisation:** `IStringLocalizer` with `.resx` files in `Resources/`. Support `en` and `de`.
- **Styling:** Bootstrap 5. Don't introduce other CSS frameworks.
- **No comments** unless the logic is genuinely non-obvious. Code should be self-explanatory.
- **No docstrings** on standard CRUD methods. Document the PARIS parser logic where needed.

---

## Key files to know

| File | Purpose |
|---|---|
| `src/Accord.Web/Program.cs` | App entry point, DI registration, middleware pipeline |
| `src/Accord.Web/Infrastructure/Data/AppDbContext.cs` | EF Core context |
| `src/Accord.Web/Domain/Parser/ParisParser.cs` | Core PARIS parsing logic |
| `src/Accord.Web/Services/AuthService.cs` | Magic Link generation + validation |
| `src/Accord.Web/Components/Pages/Eiffel/Elicitation.razor` | Core product feature |
| `docs/templates/paris/` | PARIS template JSON definitions (do not modify) |
| `PLAN.md` | Phased implementation plan — check task status here |
| `AGENTS_HISTORY.md` | Session history — read first, update at end |

---

## What is deliberately out of scope

Do not implement these, even if they seem useful:

- Per-user template management (create/edit/delete templates or template sets)
- OAuth2 or any external identity provider
- User profile editing
- Admin interface
- Template versioning per user
- Per-project requirement organisation (flat scratch-pad list is sufficient)
- Requirement editing or versioning
