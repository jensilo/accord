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

## Phase 5 — Easy Templates

Goal: A family of low-barrier, broadly useful templates that complement PARIS and make Accord accessible to users unfamiliar with formal requirements languages.

### Background and decisions

Easy templates reuse the existing `TemplateConfig` JSON format and parser — no parser changes are needed. They are structured under `docs/templates/easy-en/` and `docs/templates/easy-de/` as separate language families (type `easy-en` / `easy-de`). Separating by language as distinct families keeps things simple and allows the EN and DE variants to evolve independently over time.

The seeder is generalised from a 2-level scan (`version → files`) to a 3-level scan (`family → version → files`), with `DefinitionsPath` defaulting to `docs/templates/`. A new `Family` field is added to `TemplateSet` so the picker UI can group and filter by family without fragile string-prefix matching.

The template picker in `Elicitation.razor` gains family filter chips ("All | PARIS | Easy (EN) | Easy (DE)") rendered dynamically from distinct `Family` values — adding a new family later costs no UI code. `TemplateService` is updated to return only the latest version of each set (lexicographic sort), so older seed versions are retained in the DB but hidden from the picker.

### Templates to define

Easy (EN) — `docs/templates/easy-en/v1.0.0/`:

- `user-story.json` — "As a [role], I want [capability], so that [benefit]." (with/without benefit variant)
- `freetext.json` — Single unconstrained text field; no validation; useful as a scratch-pad or for non-standard needs
- `goal.json` — "[The system] shall [achieve goal]." with optional rationale
- `function.json` — "[The system] shall [action] [object]." functional requirement
- `quality.json` — "[The system] shall [quality dimension] [criterion]." non-functional requirement
- `glossary.json` — "[Term]: [Definition]." glossary entry

Easy (DE) — `docs/templates/easy-de/v1.0.0/`:

- `nutzererlebnis.json` — German User Story
- `freitext.json` — German free-text
- `ziel.json` — German Goal (distinct from PARIS `ziel` — no PARIS syntax constraints)
- `funktion.json` — German Function
- `qualitaet.json` — German Quality
- `glossar.json` — German Glossary entry

### Phase 5 tasks

- [x] Fix PARIS template JSON version mismatch: update `"version"` in all `docs/templates/paris/v0.6.2/*.json` from `0.1.0` to `0.6.2` so the per-template version matches the directory/set version; add an EF migration (or seeder cleanup pass) to delete the stale `0.1.0` `Template` rows from the database
- [x] Add `Family` string to `TemplateSet` entity + EF Core migration
- [x] Generalise `TemplateSeeder` to 3-level scan (`docs/templates/` root → family dir → version dir → files); derive `Family` from directory name; update `TemplateOptions.DefinitionsPath` default to `docs/templates`
- [x] Update `TemplateService.GetAllTemplatesAsync()` (or equivalent) to return only the latest-version templates per set
- [x] Define Easy (EN) template JSON files (6 templates listed above)
- [x] Define Easy (DE) template JSON files (6 templates listed above)
- [x] Add family filter chips to `Elicitation.razor` template picker modal (dynamic, based on distinct `Family` values); combine with existing full-text search
- [x] Add localisation strings for Easy template family labels (EN/DE)
- [x] Verify parser behaviour with Easy template structures (no parser changes expected)

**Done when:** All Easy (EN) and Easy (DE) templates are seeded and appear in the picker under their respective filter. The PARIS filter shows existing PARIS templates. Full-text search respects the active family filter. A user can elicit a requirement using any Easy template.

---

## Phase 6 — Documentation Restructuring

Goal: Organised `docs/` directory as the canonical reference for Accord's requirements, architecture, and template family documentation.

### Structure

```text
docs/
  README.md                     index and navigation
  architecture.md               moved from ARCHITECTURE.md in root (root file deleted)
  easy-templates.md             concept, rationale, and template overview for Easy family
  requirements/
    requirements-001.md         initial Accord requirements (Phase 1–4 scope)
    requirements-002.md         Easy templates requirements (Phase 5 scope)
  templates/
    paris/v0.6.2/               unchanged
    easy-en/v1.0.0/             seeded in Phase 5
    easy-de/v1.0.0/             seeded in Phase 5
```

### Phase 6 tasks

- [ ] Create `docs/README.md` — index linking to architecture, requirements, and template family docs
- [ ] Move `ARCHITECTURE.md` to `docs/architecture.md`; update `README.md` root reference; delete `ARCHITECTURE.md` from root
- [ ] Create `docs/requirements/requirements-001.md` — captures the initial scope (Phases 1–4): what Accord is, what was built, and what was deliberately left out
- [ ] Create `docs/requirements/requirements-002.md` — captures Phase 5 scope: Easy templates rationale, the six EN and six DE templates, filter UI, seeder changes
- [ ] Create `docs/easy-templates.md` — explains the Easy template concept, intended audience, design principles, and list of templates
- [ ] Update `CLAUDE.md` key files table to reference `docs/architecture.md`

**Done when:** `docs/` is a coherent, navigable documentation project. New contributors and agents can find requirements, architecture, and template family guidance from `docs/README.md`.

---

## Out of scope (deliberately cut)

- Per-user template management (create/edit/delete template sets or templates)
- Template copying between sets
- User profile editing
- OAuth2 / external identity providers
- Admin interface
- Template versioning per user
