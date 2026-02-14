# Service Registration Layering

This document explains the intentional dual registration of SSO, MIS, and ASM services across Infrastructure and Business layers. This is **by design** and does not represent a conflict.

## Overview

Both `HttpClientRegistrationExtensions` (Infrastructure) and `Business/DependencyInjection` (Business) register services with similar names: `ISsoService`, `IMisService`, `IAsmService`. These are **different interfaces** in different namespaces.

## Layer Responsibilities

| Layer | Interface | Implementation | Purpose |
|-------|-----------|----------------|---------|
| **Core** | `WebShop.Core.Interfaces.Services.ISsoService` | `SsoService` (Infrastructure) | Raw HTTP calls to external SSO API |
| **Core** | `WebShop.Core.Interfaces.Services.IMisService` | `MisService` (Infrastructure) | Raw HTTP calls to external MIS API |
| **Core** | `WebShop.Core.Interfaces.Services.IAsmService` | `AsmService` (Infrastructure) | Raw HTTP calls to external ASM API |
| **Business** | `WebShop.Business.Services.Interfaces.ISsoService` | `SsoService` (Business) | Wraps Core with caching, DTO mapping |
| **Business** | `WebShop.Business.Services.Interfaces.IMisService` | `MisService` (Business) | Wraps Core with caching, DTO mapping |
| **Business** | `WebShop.Business.Services.Interfaces.IAsmService` | `AsmService` (Business) | Wraps Core with logging, DTO mapping |

## Registration Locations

- **Infrastructure** (`HttpClientRegistrationExtensions.AddInfrastructureHttpClients`): Registers Core interfaces with Infrastructure implementations (HTTP clients, resilience).
- **Business** (`DependencyInjection.AddBusinessServices`): Registers Business interfaces with Business implementations (which depend on Core interfaces).

## Dependency Flow

```
API Controllers
    → Business.Services.Interfaces.ISsoService (injected)
        → Business.Services.SsoService
            → Core.Interfaces.Services.ISsoService (injected)
                → Infrastructure.Services.External.SsoService (HTTP client)
```

## Why This Design?

1. **Separation of concerns**: Infrastructure handles HTTP; Business handles caching and domain logic.
2. **Testability**: Controllers and Business services can be tested with mocks of Business interfaces.
3. **No circular dependency**: Business depends on Core; Infrastructure implements Core. Business wraps Core services.

## For New Developers

When you see both `HttpClientRegistrationExtensions` and `Business/DependencyInjection` registering "the same" services, remember: they register **different interfaces**. There is no conflict. The API layer consumes only the Business interfaces.
