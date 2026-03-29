# Accord

**Accord** is a ground-up rewrite of [HARMONY](https://github.com/org-harmony/harmony), a tool for template-based requirements elicitation — in particular for Reduced Natural Languages such as [PARIS](https://www.researchgate.net/publication/363630019_Anforderungen_strukturiert_mit_Schablonen_dokumentieren_in_PARIS).

## What is this?

HARMONY was originally implemented in Go. While Go is a solid language, the original implementation accumulated friction over time: it felt unidiomatic, the architecture was not well-suited for object-oriented thinking, and it became increasingly hard for students and contributors to maintain or extend.

Accord addresses this directly.

## Why the rewrite?

| Concern | Decision |
|---|---|
| Go's functional/structural style doesn't map well to requirements domain concepts (entities, relationships, rules) | Rewrite in **C# / .NET** — OOP is a natural fit for this domain |
| Complex frontend with HTMX partial rendering + hand-rolled Go templates | Replace with **Blazor Server** — interactive components without the complexity |
| OAuth2 with external providers — unnecessary for an academic tool | Replace with **Magic Link** email authentication — simple, no external dependencies |
| Per-user template management — rarely used, high complexity | Remove — default PARIS templates are served to all users |
| Too many moving parts for a focused elicitation tool | **KISS** — strip to the core: authenticate, pick a template, elicit a requirement |

## What it does

Accord allows users to write structured requirements based on templates — particularly the **PARIS** pattern language. The elicitation interface (EIFFEL) guides users through template variants, validates inputs in real time, and produces well-formed requirement statements. Requirement data never leaves the browser.

## Stack

- **C# / .NET 10** — backend and application logic
- **Blazor Server** — interactive frontend, server-side rendered components
- **Entity Framework Core** — data access (PostgreSQL via Npgsql)
- **Bootstrap 5** — styling (consistent with the original)
- **MailKit** — Magic Link email delivery

## Status

Active development. See [PLAN.md](PLAN.md) for current progress and [ARCHITECTURE.md](ARCHITECTURE.md) for technical design.
