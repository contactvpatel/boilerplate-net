# Documentation Audit Report

**Date**: February 5, 2026  
**Scope**: All files under `docs/` checked against project documentation guidelines and cross-references.

---

## Summary

The docs were audited for:

1. **Consistency with doc conventions**: "[← Back to README]" link, Table of Contents where appropriate
2. **Accuracy of `docs/README.md`**: Index matching actual files and locations
3. **Broken internal links** between docs (cross-folder references)
4. **References to non-existent files**

---

## Issues Found and Fixed

### 1. docs/README.md

- **Issue**: `allowed-hosts.md` was listed under **both** Architecture and Guides. The file exists only in `architecture/allowed-hosts.md`.
- **Fix**: Removed the duplicate entry from the Guides section.

### 2. Missing "[← Back to README]" Link

- **Issue**: Three testing docs did not include the standard back link to the documentation index.
- **Fix**: Added `[← Back to README](../README.md)` to:
  - `testing/unit-testing.md`
  - `testing/dapper-testing-guide.md`
  - `testing/testing-comprehensive-guide.md`

### 3. Broken Cross-Document Links

Links that pointed to the wrong folder (relative paths resolved in the current folder only) were corrected:

| File | Broken Link | Fixed To |
|------|-------------|----------|
| `architecture/project-structure.md` | `dapper-testing-guide.md`, `httpclient-factory.md`, `performance-optimization-guide.md` | `../testing/...`, `../guides/...` |
| `architecture/dapper-hybrid-approach.md` | `dapper-testing-guide.md`, `performance-optimization-guide.md`, `database-connection-settings-guidelines.md` | `../testing/...`, `../guides/...`, `../standards/...` |
| `architecture/cors.md` | `api-versioning-guidelines.md` | `../standards/api-versioning-guidelines.md` |
| `architecture/resilience.md` | `httpclient-factory.md` | `../guides/httpclient-factory.md` |
| `architecture/jwt-authentication-filter.md` | `./caching.md` | `../guides/hybrid-caching.md` |
| `standards/naming-guidelines.md` | `project-structure.md` | `../architecture/project-structure.md` |
| `guides/httpclient-factory.md` | `resilience.md`, `validation-filter.md`, `jwt-authentication-filter.md`, `caching.md` | `../architecture/...`, `hybrid-caching.md` |

### 4. References to Non-Existent Files

- **`httpclient-lifecycle.md`**: Referenced in `guides/httpclient-factory.md` but file does not exist. Replaced with in-doc references (content is covered in the same guide).
- **`caching.md`**: Does not exist; project has `hybrid-caching.md`. Links updated to `hybrid-caching.md` or `../guides/hybrid-caching.md`.
- **`testing-strategy-base.md`**, **`testing-strategy.md`**: Referenced in `testing/testing-comprehensive-guide.md` as superseded; files not in repo. Removed from Resources and added link to `dapper-testing-guide.md` instead.
- **`.ai/coding-conventions.md`**: Referenced in `standards/naming-guidelines.md`; `.ai/` not present. Link removed from Related Documentation.
- **`../.ai/sql-guidelines.md`**: Referenced in `guides/dbup-migrations.md`; removed.

### 5. Table of Contents Added

- **`architecture/project-structure.md`**: Added TOC (long doc with many sections).
- **`testing/dapper-testing-guide.md`**: Added TOC for consistency and navigation.

---

## Optional Follow-Up (Not Done)

These are consistent with the rest of the docs but could be improved later:

- **Table of Contents**: Several other long docs do not have a TOC (e.g. `logging-strategy-recommendations.md`, `explicit-type-usage-guidelines.md`, `dapper-hybrid-approach.md`, `rate-limiting.md`, `performance-optimization-guide.md`, `response-caching-implementation.md`, `mapster-code-generation-guide.md`). Adding TOC would match the pattern used in standards and some architecture/guides.
- **Archive docs**: `archive/test-categorization-audit-plan.md` and `archive/test-codebase-comprehensive-audit.md` do not have a Back to README link; optional for historical docs.

---

## Files Touched

- `docs/README.md`
- `docs/testing/unit-testing.md`
- `docs/testing/dapper-testing-guide.md`
- `docs/testing/testing-comprehensive-guide.md`
- `docs/architecture/project-structure.md`
- `docs/architecture/dapper-hybrid-approach.md`
- `docs/architecture/cors.md`
- `docs/architecture/resilience.md`
- `docs/architecture/jwt-authentication-filter.md`
- `docs/standards/naming-guidelines.md`
- `docs/guides/httpclient-factory.md`
- `docs/guides/dbup-migrations.md`

This audit report can be moved to `docs/archive/` after review if desired.
