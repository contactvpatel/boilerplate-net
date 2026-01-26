# Documentation Index

This directory contains all technical documentation for the Boilerplate .NET project, organized by category.

## 📂 /standards
**Mandatory guidelines that define how we write code. These ensure consistency, maintainability, and quality across the codebase.**

- **`api-response-formats.md`**: Defines the standard JSON structure (`Success`, `Data`, `Errors`) for all API responses. Critical for frontend integration consistency.
- **`api-versioning-guidelines.md`**: Rules for versioning endpoints (v1, v2) to ensure backward compatibility as the API evolves.
- **`cancellation-token-guidelines.md`**: Mandatory usage of `CancellationToken` in async methods to prevent wasted resources on cancelled requests.
- **`collection-types-guidelines.md`**: Best practices for using `IEnumerable`, `List`, and `Array` to optimize memory and performance.
- **`database-connection-settings-guidelines.md`**: Standards for configuring connection strings (timeouts, pooling) for stability.
- **`explicit-type-usage-guidelines.md`**: Rules for when to use `var` vs explicit types to maximize code readability.
- **`logging-strategy-recommendations.md`**: Standards for log levels (Info vs Debug), structured logging, and sensitive data masking.
- **`naming-guidelines.md`**: Naming conventions for classes, interfaces, and variables (PascalCase, camelCase) to match .NET standards.
- **`response-compression-guidelines.md`**: Configuration for GZIP/Brotli compression to reduce bandwidth usage.
- **`xml-comments-guidelines.md`**: Requirements for documenting public APIs so that Swagger/OpenAPI documentation is automatically generated.

## 📂 /architecture
**High-level design decisions and cross-cutting concerns. Explains "why" the system works the way it does.**

- **`allowed-hosts.md`**: Configuring the `AllowedHosts` setting to prevent host header attacks.
- **`asm-authorization.md`**: Implementation guide for the Authorization Service Module integration.
- **`content-security-policy.md`**: Security headers configuration to prevent XSS and other injection attacks.
- **`cors.md`**: Cross-Origin Resource Sharing policies defining which domains can access the API.
- **`dapper-hybrid-approach.md`**: Explains the "Hybrid Dapper" architecture which combines **direct Dapper** for high-performance reads and a **shared base repository** for consistent writes (using Dapper), avoiding the overhead of EF Core.
- **`db-schema-diagram.md`**: Overview of the core database schema and relationships.
- **`exception-handling.md`**: The global error handling strategy, including custom exceptions and how the middleware processes them.
- **`health-checks.md`**: Implementation of health probe endpoints for Kubernetes/container orchestration.
- **`idempotency-analysis.md`**: Strategy for handling duplicate requests safely, critical for payment/transaction integrity.
- **`jwt-authentication-filter.md`**: How the custom JWT auth filter works compared to standard middleware (performance/customization reasons).
- **`opentelemetry-integration.md`**: Setup for distributed tracing and metrics (Prometheus/Grafana/Jaeger).
- **`project-structure.md`**: The "map" of the solution (API, Business, Core, Infrastructure layers) and dependency flow.
- **`rate-limiting.md`**: Throttling policies to protect the API from abuse and DDoS attacks.
- **`resilience.md`**: Global resilience strategies including retry policies, circuit breakers, and timeouts.
- **`security-configuration-comparison.md`**: Comparison of security settings across different environments (Dev vs Prod).
- **`validation-filter.md`**: How the automatic model validation pipeline works (FluentValidation integration).

## 📂 /guides
**"How-to" guides and implementation details. Explains "how" specific features are built.**

- **`allowed-hosts.md`**: Configuring the `AllowedHosts` setting to prevent host header attacks.
- **`database-migration-postgresql-to-sqlserver.md`**: Guide for porting the database engine if required.
- **`dbup-migrations.md`**: **Current** database migration workflow using DbUp (SQL-first approach).
- **`efcore-migration-guide.md`**: **Future/Alternative** guide for migrating to EF Core migrations if the team decides to switch.
- **`httpclient-factory.md`**: Comprehensive guide on using `IHttpClientFactory` to prevent socket exhaustion, including connection pooling and lifecycle management.
- **`hybrid-caching.md`**: Implementation of server-side `HybridCache` (combining in-memory and Redis).
- **`mapster-code-generation-guide.md`**: How to use Mapster for high-performance object-to-object mapping.
- **`performance-optimization-guide.md`**: Checklist and techniques used to tune API performance (allocations, async hot-paths).
- **`response-caching-implementation.md`**: Client-side caching strategy using HTTP cache headers (distinct from server-side caching).
- **`scalar-ui.md`**: Guide to using Scalar UI as an alternative to Swagger UI for API testing.

## 📂 /testing
**Testing strategies, coverage requirements, and audit reports.**

- **`dapper-testing-guide.md`**: Specific patterns for unit testing Dapper repositories using mock connections.
- **`testing-comprehensive-guide.md`**: The master strategy document defining the testing pyramid (Unit/Integration/E2E) and coverage targets.
- **`unit-testing.md`**: Developer guide for writing effective unit tests with xUnit, Moq, and FluentAssertions.

## 📂 /archive
**Historical documents and one-time reports. Kept for reference but may not reflect current state.**

- **`test-categorization-audit-plan.md`**: Jan 2026 audit verifying that all unit tests were correctly categorized.
- **`test-codebase-comprehensive-audit.md`**: Full audit report of the test suite status as of Jan 2026.
