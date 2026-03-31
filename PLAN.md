# Implementation Plan

> This directory is self-contained and will be moved to its own repository. All paths are relative to `accord/`.

This document tracks the phased implementation of Accord. Phases are sequential — each phase produces something runnable before moving to the next.

---

## Phase 1 — Foundation

Goal: Running ASP.NET Core app with database, auth, and base layout.

- [x] Scaffold `Accord.Web` project (Blazor Server, .NET 10)
- [x] Configure EF Core + Npgsql, `AppDbContext`
- [x] Define `User` and `MagicLinkToken` entities
- [x] Initial EF Core migration
- [x] Implement `AuthService` (token generation, validation)
- [x] Implement `MailService` (MailKit, configurable SMTP)
- [x] Wire up ASP.NET Core cookie authentication
- [x] `Login.razor` — email input form
- [x] `Verify.razor` — token landing, session issuance (handled by `/auth/verify` minimal API endpoint)
- [x] `MainLayout.razor` — base shell with Bootstrap 5, nav
- [x] `Home.razor` — landing page (conditional CTA based on auth state)
- [x] Docker setup (dev + production compose)
- [x] `appsettings.json` configuration structure

**Done when:** A user can enter their email, receive a magic link, click it, and be logged in with a session cookie. The app runs in Docker.

---

## Phase 2 — Templates

Goal: PARIS templates available in the database, browsable in the UI.

- [x] Define `TemplateSet` and `Template` entities
- [x] EF Core migration for template tables
- [x] `TemplateSeeder` — idempotent load from `docs/templates/paris/` JSON files
- [x] Copy PARIS template JSON definitions from HARMONY (`docs/templates/paris/v0.6.2/`)
- [x] `TemplateConfig` C# model — deserialize PARIS JSON (`rules`, variants, placeholders)
- [x] `ITemplateService` / `TemplateService` — query template sets and templates
- [x] Template list page (read-only, all users see the same sets)
- [x] Template detail view

**Done when:** All PARIS templates from HARMONY are seeded in the database and browsable in the UI.

---

## Phase 3 — EIFFEL Elicitation

Goal: Core product feature — interactive requirements elicitation.

- [x] Port `ParisParser` from Go to C# (`Domain/Parser/`)
  - Segment-based parsing
  - Rule validation (required, optional, `ignoreMissingWhenOptional`)
  - `ParsingResult` with errors, warnings, notices
- [x] `Elicitation.razor` — main elicitation page
  - Template set + template search/filter
  - Variant picker
  - Dynamic form rendering driven by `TemplateConfig.Rules`
  - Display types: text input, textarea, single select
  - Real-time parsing on input change
  - Requirement output display
  - Error/warning/notice display
- [x] `IRequirementService` / `RequirementService` — save requirement text per user (flat list, no grouping)
- [x] `Requirement` entity + EF Core migration
- [x] Save button in elicitation UI → persists to DB
- [ ] Saved requirements list (simple read view per user)
- [x] Navigation registration for EIFFEL

**Done when:** A user can select a PARIS template, fill in the fields, see a generated requirement update in real time, and optionally save it. The dynamic form correctly handles all PARIS rule types and variant switching.

---

## Phase 4 — Localisation & Polish

Goal: German + English language support, consistent styling, production-ready.

- [x] `.resx` resource files for English and German
- [x] `IStringLocalizer` wired into components
- [x] Translate all UI strings (port from HARMONY `translations/en.json` and `de.json`)
- [x] Language detection from browser `Accept-Language` header
- [x] Error pages (404, 500)
- [x] Review Bootstrap usage — consistent look matching HARMONY's general feel
- [x] Production Docker image (multi-stage, non-root user)
- [ ] Environment variable documentation

**Done when:** The app is fully bilingual, stable, and deployable.

---

## Out of scope (deliberately cut)

- Per-user template management (create/edit/delete template sets or templates)
- Template copying between sets
- User profile editing
- OAuth2 / external identity providers
- Admin interface
- Template versioning per user
