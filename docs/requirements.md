# Accord — System Requirements

Accord is a web-based tool that supports users in writing structured requirements using template-based pattern languages, in particular [**PARIS** (PAtterns for RequIrements Specification)](https://www.researchgate.net/publication/363630019_Anforderungen_strukturiert_mit_Schablonen_dokumentieren_in_PARIS). It is a rewrite of [HARMONY/EIFFEL](https://github.com/org-harmony/harmony), retaining the core elicitation functionality while removing complexity that did not serve the primary use case.

---

## Stakeholders

| Stakeholder | Description |
| --- | --- |
| Anonymous user | A person who has not yet authenticated. May access the login page. |
| User | An authenticated person who uses the system to elicit requirements. |
| Operator | The person or team hosting and maintaining the Accord instance. |

---

## Goals

**REQ-1** — Accord must support requirements elicitation using template-based pattern languages, so that users can write well-formed requirement statements more easily.

**REQ-2** — Accord must be usable without prior knowledge of the internal structure of template definitions, so that using the tool does not require understanding template syntax.

**REQ-3** — Accord must be operable without a user manual or dedicated training.

**REQ-4** — Accord must support English and German as user interface languages, so the tool can be used outside German-speaking contexts.

**REQ-5** — Accord must be designed to be extensible through further development phases, so it can serve as the basis for student thesis projects.

**REQ-6** — Accord must be usable from a web browser without the user installing any software.

**REQ-7** — Accord must run in an isolated container, so it does not conflict with other services on the same host.

**REQ-8** — The deployment of Accord and its dependencies must be reproducible with a standard configuration, so the system can be set up in different environments with minimal effort.

---

## Non-functional Requirements

**REQ-9** — Accord must be operable in current versions of Firefox.

**REQ-10** — Accord must be operable in current versions of Chromium-based browsers.

**REQ-11** — Accord must sanitize all user input, so that malicious content cannot be injected into the application.

**REQ-12** — Accord must display a notice that it is a research prototype and is used at the user's own risk.

**REQ-13** — Accord must have a low resource footprint (disk and memory), so it can run on modest hardware.

---

## Authentication

**REQ-14** — The system must allow a user to authenticate by entering only an email address, without requiring a password or an external identity provider.

**REQ-15** — After a user submits their email address, the system must send a one-time login link to that address.

**REQ-16** — A login link must expire after 15 minutes.

**REQ-17** — A login link must be invalidated after it is used once, so it cannot be replayed.

**REQ-18** — When a user follows a valid login link, the system must authenticate them and issue a session cookie.

**REQ-19** — If no account exists for the submitted email address, the system must create one automatically, so the user can log in without prior registration.

---

## Template Management

**REQ-20** — All PARIS template definitions must be available to all authenticated users, so no per-user template setup is required.

**REQ-21** — Templates must be loaded from definition files at application startup.

**REQ-22** — The template loading process must be idempotent, so restarting the application does not duplicate template data.

**REQ-23** — Templates must be organized into named, versioned template sets.

**REQ-24** — Each template must carry a name, version, and a configuration that defines its rules and variants.

**REQ-25** — Each template variant must carry a name, description, format string, and usage example, so users understand how to apply it correctly.

**REQ-26** — Users must not be able to create, edit, delete, or archive templates or template sets.

---

## Elicitation Interface

### Template Selection

**REQ-27** — The system must allow the user to select a template from the list of available templates.

**REQ-28** — After the user completes a requirement entry, the previously selected template must remain active, so they do not need to re-select it for each subsequent entry.

### Dynamic Input Form

**REQ-29** — When a template and variant are selected, the system must automatically render an input form with fields derived from the template configuration, so the user is not required to know the template's internal structure.

**REQ-30** — The display type of each input field (text input, text area, dropdown) must be determined by the template configuration.

**REQ-31** — The width of each input field must be configurable per rule in the template configuration (e.g. small, medium, full-width), so field widths match the expected length of the input.

**REQ-32** — Required fields must be visually distinguished from optional fields without relying on color alone, so users with color vision deficiency are not disadvantaged.

**REQ-33** — The form must be fully navigable using only the keyboard, so the user can move between fields without a mouse.

**REQ-34** — Each input field must be able to display a brief explanatory hint on demand (e.g. via an info popover), so the user knows what to enter in that field.

### Variant Switching

**REQ-35** — The system must allow the user to switch between the variants of the selected template during input.

**REQ-36** — When the user switches variants, the system must replace the active input form entirely with the form for the new variant.

**REQ-37** — The user must be able to switch variants using a keyboard shortcut, so no mouse interaction is required.

**REQ-38** — If a template variant has hints defined in its configuration, the system must allow the user to view those hints during elicitation, so the variant is easier to apply correctly.

### Real-time Parsing and Validation

**REQ-39** — On every input change, the system must validate the current form values against the selected template and update the result immediately, so no manual submit action is needed.

**REQ-40** — The system must pre-process user input before validation to remove excess whitespace.

**REQ-41** — If validation detects errors, the system must display them, so the user knows what to correct.

**REQ-42** — If validation detects non-critical issues, the system must display improvement suggestions, so the user can increase requirement quality.

**REQ-43** — If the current input conforms to the template, the system must display a positive confirmation.

**REQ-44** — The template validation engine must support an Equals rule, which requires a fixed string to appear at a specific position in the requirement text.

**REQ-45** — The template validation engine must support an EqualsAny rule, which requires one of a predefined list of strings to appear at a given position.

**REQ-46** — An EqualsAny rule must support an `allowOthers` option, so that values outside the predefined list are also accepted when the option is enabled.

**REQ-47** — The template validation engine must support a Placeholder rule, which accepts free text at a given position.

**REQ-48** — Any rule in a template may be marked as optional, so violating it does not invalidate the requirement.

**REQ-49** — When an optional rule is violated, the system must suppress the corresponding notice if the template configuration requests it, so the user is not unnecessarily prompted about absent optional clauses.

### Requirement Output

**REQ-50** — The system must display the assembled requirement text in real time as the user fills in the form, combining field values with the template's fixed text.

**REQ-51** — The system must allow the user to copy the assembled requirement text to the operating system clipboard, so it can be transferred directly into another tool.

**REQ-52** — After the requirement text has been copied, the system must automatically clear the input form, so the user can immediately begin entering the next requirement.

---

## Requirements Storage

**REQ-53** — The system must allow an authenticated user to save a confirmed requirement.

**REQ-54** — Saved requirements must be stored as a flat list per user, without project grouping or folder structure.

**REQ-55** — Each saved requirement must record the template with which it was created.

---

## Out of Scope

The following are explicitly not in scope for Accord and must not be implemented:

- Per-user template management (create, edit, delete, or archive templates or template sets)
- OAuth2 or any external identity provider
- User profile editing
- Administrative interface
- Per-project or per-group organisation of requirements
- Requirement editing or versioning
- Tracing between requirements
- Integration with third-party project management or office tools
- Collaboration features (sharing, multi-user editing)
