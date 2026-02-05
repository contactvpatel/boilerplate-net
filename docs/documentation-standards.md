# Documentation Standards

This document defines how documentation in this repository is organized, structured, and written so that all docs follow the same pattern for reading consistency and align with Microsoft and industry practices.

[← Back to README](README.md)

## Table of Contents

- [Organization](#organization)
- [Document Template](#document-template)
- [Content Guidelines](#content-guidelines)
- [Cross-References and Duplication](#cross-references-and-duplication)
- [Microsoft and Industry Alignment](#microsoft-and-industry-alignment)
- [Checklist for New or Updated Docs](#checklist-for-new-or-updated-docs)

---

## Organization

Documentation is split by **purpose**, not by feature:

| Folder | Purpose | Audience | Example |
|--------|---------|----------|---------|
| **`/standards`** | Mandatory rules for how we write code. Must-follow. | All developers | Naming, API response format, cancellation tokens |
| **`/architecture`** | Design decisions and cross-cutting concerns. Explains *why*. | Architects, senior devs | Exception handling, Dapper approach, health checks |
| **`/guides`** | How-to and implementation steps. Explains *how*. | Developers implementing features | DbUp migrations, HttpClient factory, Mapster |
| **`/testing`** | Testing strategy, coverage, and test patterns. | Developers, QA | Unit testing guide, testing comprehensive guide |
| **`/archive`** | Historical or one-time reports. May be outdated. | Reference only | Audit reports, superseded plans |

- **Index**: [`docs/README.md`](README.md) is the single entry point and must list every doc with a one-line description.
- **One topic per doc**: Each file covers a single topic so readers know where to look.

---

## Document Template

Every doc (except the index and archive) should follow this structure for **reading consistency**:

```markdown
# Document Title

[← Back to README](path/to/README.md)

## Table of Contents

- [Section One](#section-one)
- [Section Two](#section-two)
- ...

---

## Overview

Brief purpose of the doc and what the reader will learn. One short paragraph is enough.

## Section One
...
```

### Rules

1. **Title**: One `#` heading matching the file topic (e.g. "API Response Formats Guide").
2. **Back link**: Second element. Use `[← Back to README](../README.md)` from `testing/` or `archive/`, or `[← Back to README](../../README.md)` from `standards/`, `architecture/`, or `guides/`.
3. **Table of Contents**: For any doc with more than three `##` sections. List all `##` headings; anchors are lowercase, spaces become hyphens, punctuation removed (e.g. `## Decision Matrix: What to Log` → `#decision-matrix-what-to-log`).
4. **Horizontal rule**: `---` between TOC and first section.
5. **Overview**: First section after TOC. One short paragraph stating purpose and scope.
6. **Sections**: Use `##` for main sections, `###` for subsections. Prefer consistent patterns (e.g. "Why X?", "What problem it solves", "How it works", "Implementation details", "Best practices", "Troubleshooting").
7. **Related docs**: End with a "Related Documentation" or "References" section linking to other docs and external sources (Microsoft, RFCs) instead of duplicating their content.

Short docs (e.g. a single diagram) may omit the TOC but must keep the Back link and a brief intro.

---

## Content Guidelines

- **Detail**: Be specific enough that a new team member can follow the doc without guessing. Include:
  - When to use the feature or guideline
  - Concrete examples (code, config, or commands)
  - Configuration and troubleshooting where relevant
- **Clarity**: Use short sentences and bullet lists. Avoid long walls of text.
- **Accuracy**: Keep file paths, namespaces, and code samples in sync with the codebase. Prefer referencing the repo over pasting large blocks that can go stale.
- **Tone**: Neutral and instructional. Use "MUST" / "SHOULD" / "MAY" in standards; "recommended" / "optional" in guides.

---

## Cross-References and Duplication

- **Single source of truth**: Define each concept in one place. Other docs should *link* to it, not re-explain it in full.
  - Example: The canonical API response shape is in `standards/api-response-formats.md`. `architecture/exception-handling.md` should describe how exceptions map to that format and link to the standards doc for the full schema.
- **Related Documentation**: Each doc should end with links to:
  - Other docs in this repo that extend or depend on the topic
  - Official Microsoft or industry references (see below)
- **Avoid**: Copy-pasting the same tables, JSON samples, or long explanations into multiple files. Use a link and a one-line summary instead.

---

## Microsoft and Industry Alignment

Docs should align with:

- **Microsoft Learn**: Where applicable, cite [learn.microsoft.com](https://learn.microsoft.com) (e.g. naming, async, health checks, resilience). Prefer "aligns with" or "extends" rather than re-writing Microsoft’s content.
- **.NET guidelines**: Follow [.NET coding guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and project-specific conventions documented in `/standards`.
- **Industry**: For HTTP, security, and APIs, reference common standards (e.g. RFCs, OWASP, REST best practices) in a "References" section rather than duplicating them.
- **Explicit precedence**: In `/standards`, state that Microsoft’s or the referenced standard’s guidance takes precedence when there is a conflict (e.g. as in `naming-guidelines.md`).

This keeps the doc set consistent with Microsoft and industry standards without duplicating external content.

### Verification checklist (per doc)

- **Standards** (`/standards`): State that [Microsoft’s guidance](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) (or the cited standard) **takes precedence** when in conflict. Include a **References** section with at least one `learn.microsoft.com` link where applicable.
- **Architecture & guides**: Include a **References** or **Related Documentation** section that links to:
  - Microsoft Learn for ASP.NET Core / .NET topics (e.g. [learn.microsoft.com/aspnet/core](https://learn.microsoft.com/en-us/aspnet/core/)).
  - RFCs, OWASP, or W3C/MDN where relevant (HTTP, security, APIs).
- **URLs**: Prefer **learn.microsoft.com** over legacy **docs.microsoft.com** for Microsoft links.

---

## Checklist for New or Updated Docs

- [ ] Placed in the correct folder (standards / architecture / guides / testing / archive).
- [ ] Listed in `docs/README.md` with a one-line description.
- [ ] Uses the template: Title → Back to README → TOC (if 4+ sections) → `---` → Overview → sections.
- [ ] Overview is one short paragraph; no long intro before the Back link.
- [ ] All `##` sections appear in the TOC with correct anchors.
- [ ] "Related Documentation" or "References" includes links to other docs and to Microsoft/industry sources where relevant.
- [ ] No large duplication of content that already exists in another doc; use cross-links instead.
- [ ] Code/config examples and paths match the current codebase.
