# WebShop .NET API Boilerplate

A clean architecture boilerplate for building .NET 10 Web APIs with PostgreSQL. This solution provides a comprehensive foundation with best practices, security, performance optimizations, and extensive documentation.

**Perfect for:** Developers building new APIs, teams adopting Clean Architecture, and projects requiring enterprise-grade patterns.

## What's Included

- ✅ **Clean Architecture** - Proper separation of concerns across layers
- ✅ **High-Performance Data Access** - Hybrid Dapper approach (3-5x faster than generic repositories)
- ✅ **Production-Ready** - Security, performance, resilience patterns built-in
- ✅ **Comprehensive Documentation** - 30+ guides covering all aspects
- ✅ **Modern Stack** - .NET 10, PostgreSQL, OpenTelemetry, Scalar UI

---

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- PostgreSQL 12 or higher
- Visual Studio 2026, VS Code, or Rider

### 1. Clone and Setup

```bash
git clone <repository-url>
cd boilerplate-net
```

### 2. Configure Database Connection

Update connection strings in `src/WebShop.Api/appsettings.json`:

```json
{
  "DbConnectionSettings": {
    "Read": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "postgres",
      "Password": "your-password"
    },
    "Write": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "postgres",
      "Password": "your-password"
    }
  }
}
```

**Note:** Database migrations run automatically on startup via DbUp. See [Database Migrations](#database-migrations) section below.

### 3. Run the Application

```bash
dotnet run --project src/WebShop.Api
```

The API will be available at:

- **HTTPS**: `https://localhost:7109`
- **API Documentation**: `https://localhost:7109/scalar`
- **OpenAPI JSON**: `https://localhost:7109/openapi/v1.json`

**First Run:** The application startup sequence:

1. **Connection validation** – Validates read and write database connections (fail-fast if unreachable)
2. **Database migrations** – Runs DbUp migrations (if `EnableDatabaseMigration` is `true`)
3. **Schema creation** – Creates the database schema if it doesn't exist
4. **Seed data** – Seeds initial data (if seed scripts are configured for your environment)

### 4. (Recommended) Run security checks locally

The project includes optional local security scanning to catch common vulnerabilities before pushing code. Run before committing:

```bash
pwsh scripts/security-audit.ps1 -Fast    # Quick vulnerability check (~30 seconds)
pwsh scripts/security-audit.ps1          # Full audit including credentials/secrets scan
```

**Note:** PowerShell Core is the recommended cross-platform approach. [Install PowerShell Core](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) if not already available.

See `/docs/standards/security-scanning.md` for complete documentation.

---

## 📚 Learning Path: From Basic to Advanced

This boilerplate is organized as a learning path. Start with the basics and progress to advanced topics as you build your understanding.

### 🟢 Level 1: Foundation & Basics

**Start here if you're new to the project or Clean Architecture.**

#### Project Structure & Organization

- **[Project Structure Guide](docs/architecture/project-structure.md)** - Understand Clean Architecture layers, folder organization, and how components interact
- **[Naming Guidelines](docs/standards/naming-guidelines.md)** - Comprehensive naming conventions following Microsoft's C# standards
- **[XML Comments Guidelines](docs/standards/xml-comments-guidelines.md)** - How to document your code effectively with XML comments

#### Database Fundamentals

- **[Database Schema Diagram](docs/architecture/db-schema-diagram.md)** - Visual ERD showing all tables, relationships, and indexes
- **[Database Connection Settings](docs/standards/database-connection-settings-guidelines.md)** - Configure read/write database connections, connection pooling, and best practices
- **[PostgreSQL to SQL Server Migration](docs/guides/database-migration-postgresql-to-sqlserver.md)** - Complete guide for migrating between database providers
- **[DbUp Migrations Guide](docs/guides/dbup-migrations.md)** - SQL-based migrations, automatic execution, and seed data management

#### Core Patterns & Performance

- **[Mapster Code Generation Guide](docs/guides/mapster-code-generation-guide.md)** - High-performance mapping with compile-time configuration
- **[Collection Types Guidelines](docs/standards/collection-types-guidelines.md)** - When to use `IReadOnlyList`, `List`, `IEnumerable`, and other collection types
- **[Explicit Type Usage Guidelines](docs/standards/explicit-type-usage-guidelines.md)** - Standards for using explicit types vs `var` for clarity and maintainability

---

### 🟡 Level 2: API Development & Validation

**Essential for building robust APIs.**

#### Request/Response Handling

- **[API Response Formats](docs/standards/api-response-formats.md)** - Complete guide to all possible API response formats, error codes, and structured responses
- **[Validation Filter Guide](docs/architecture/validation-filter.md)** - Automatic request validation using FluentValidation with standardized error responses
- **[Exception Handling Guide](docs/architecture/exception-handling.md)** - Global exception handling middleware with error correlation IDs and structured logging
- **[Response Compression](docs/standards/response-compression-guidelines.md)** - Optimize API responses with compression

#### API Design

- **[API Versioning Guidelines](docs/standards/api-versioning-guidelines.md)** - Implement API versioning with URL-based and header-based strategies
- **[Scalar UI Guide](docs/guides/scalar-ui.md)** - Modern, interactive API documentation with auto-filled parameters

#### Security Basics

- **[CORS Configuration](docs/architecture/cors.md)** - Environment-based CORS policies for cross-origin requests
- **[AllowedHosts Configuration](docs/architecture/allowed-hosts.md)** - Host header validation to prevent host header injection attacks
- **[Content-Security-Policy](docs/architecture/content-security-policy.md)** - CSP headers for XSS and clickjacking protection

---

### 🟠 Level 3: Authentication & Security

**Secure your API with enterprise-grade authentication.**

#### Authentication

- **[JWT Authentication Filter](docs/architecture/jwt-authentication-filter.md)** - Centralized JWT token authentication with SSO integration
- **[ASM Authorization Guide](docs/architecture/asm-authorization.md)** - Application Security Management with multiple permissions and logical operators

#### Security Configuration

- **[Security Configuration Comparison](docs/architecture/security-configuration-comparison.md)** - Understand AllowedHosts, CORS, CSP, and how they work together

---

### 🔵 Level 4: Performance & Optimization

**Optimize your API for production workloads.**

#### Data Access Performance

#### Data Access Performance

- **[Dapper Hybrid Approach](docs/architecture/dapper-hybrid-approach.md)** - High-performance data access with direct SQL mapping (3-5x faster than generic repositories)
- **[Performance Optimization Guide](docs/guides/performance-optimization-guide.md)** - Query optimization, connection pooling, and caching strategies
- **[Testing Guide](docs/testing/testing-comprehensive-guide.md#dapper-repository-testing)** - Dapper testing with mocked connections

#### Caching

- **[Caching Guide](docs/guides/hybrid-caching.md)** - HybridCache implementation with two-tier architecture and stampede protection
- **[Response Caching Implementation](docs/guides/response-caching-implementation.md)** - HTTP response caching with Cache-Control headers for reduced database load

#### HTTP Client Optimization

- **[HttpClient Factory Guide](docs/guides/httpclient-factory.md)** - Production-ready HTTP client with resilience, security hardening, and connection pooling
- **[HttpClient Lifecycle](docs/guides/httpclient-factory.md#deep-dive-lifecycle--connection-pooling)** - Understanding HttpClient lifecycle, disposal, and best practices

---

### 🔴 Level 5: Advanced Patterns & Resilience

**Enterprise-grade patterns for production systems.**

#### Resilience & Reliability

#### Resilience & Reliability

- **[Resilience Patterns Guide](docs/architecture/resilience.md)** - Retry, circuit breaker, timeout, and rate limiting strategies
- **[Rate Limiting Guide](docs/architecture/rate-limiting.md)** - Per-user/IP rate limiting with multiple policies and algorithms

#### Observability & Monitoring

- **[OpenTelemetry Integration](docs/architecture/opentelemetry-integration.md)** - Distributed tracing, metrics, and logging with production-ready observability
- **[Correlation ID Approach](docs/architecture/correlation-id.md)** - Using TraceId as X-Correlation-Id for request tracking
- **[Health Checks Guide](docs/architecture/health-checks.md)** - Kubernetes-ready health checks with enhanced JSON responses
- **[Logging Strategy](docs/standards/logging-strategy-recommendations.md)** - Structured logging best practices and recommendations

#### Advanced Topics

- **[Cancellation Token Guidelines](docs/standards/cancellation-token-guidelines.md)** - Proper async cancellation for responsive applications
- **[Idempotency Analysis](docs/architecture/idempotency-analysis.md)** - Implementing idempotent operations

---

## 🏗️ Architecture Overview

This project follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                  WebShop.Api (API Layer)                 │
│  Controllers, Middleware, Filters, Extensions            │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│              WebShop.Business (Application Layer)         │
│  Services, DTOs, Validators, Mappings                    │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                WebShop.Core (Domain Layer)               │
│  Entities, Interfaces (No Dependencies)                 │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│          WebShop.Infrastructure (Infrastructure Layer)    │
│  Repositories, Dapper Connections, External Services     │
└──────────────────────────────────────────────────────────┘
```

### Project Structure

```
boilerplate-net/
├── src/
│   ├── WebShop.Api/          # Presentation Layer
│   │   ├── Controllers/      # API Controllers
│   │   ├── Filters/          # Validation, JWT Auth, ASM
│   │   ├── Middleware/       # Exception Handling, API Versioning
│   │   ├── Extensions/       # Service Configuration (Core, Features, Middleware, Utilities)
│   │   ├── Models/           # API Request/Response (Response, PaginationQuery, etc.)
│   │   ├── Validators/       # API-level Validators (e.g. PaginationQueryValidator)
│   │   ├── Helpers/          # Health Checks, OpenAPI
│   │   └── DbUpMigration/    # SQL Migrations & Seeds
│   ├── WebShop.Business/     # Application Layer
│   │   ├── Services/         # Business Logic
│   │   ├── DTOs/             # Data Transfer Objects
│   │   ├── Mappings/         # Mapster Configuration
│   │   └── Validators/       # FluentValidation Rules
│   ├── WebShop.Core/         # Domain Layer
│   │   ├── Entities/         # Domain Entities
│   │   ├── Helpers/          # Cache Keys & Shared Helpers
│   │   ├── Interfaces/       # Repository & Service Contracts
│   │   └── Models/           # Domain Models (MIS, ASM, SSO, etc.)
│   ├── WebShop.Infrastructure/ # Infrastructure Layer
│   │   ├── Repositories/    # Dapper-based Repositories (Base + Entity-specific)
│   │   ├── Interfaces/       # IDapperConnectionFactory, IDapperTransactionManager
│   │   ├── Helpers/          # Dapper, HTTP, Security Helpers
│   │   ├── Services/         # External (MIS, SSO, ASM) & Internal (Cache, UserContext)
│   │   └── DependencyInjection.cs
│   └── WebShop.Util/         # Utilities
│       ├── Models/           # Configuration Options (DbConnection, Services, Rate Limiting)
│       └── Security/         # Security Utilities
├── docs/                     # Comprehensive Documentation
├── scripts/                  # Utility Scripts (Git hooks)
├── tests/                    # Unit Test Projects
│   ├── WebShop.Api.Tests/
│   ├── WebShop.Business.Tests/
│   ├── WebShop.Infrastructure.Tests/
│   └── WebShop.Util.Tests/
└── Directory.Packages.props  # Centralized Package Management
```

[See detailed project structure →](docs/architecture/project-structure.md)

---

## ✨ Key Features

### Architecture & Patterns

- ✅ **Clean Architecture** - Proper dependency flow and separation of concerns
- ✅ **Repository Pattern** - Generic repositories with transaction support
- ✅ **Soft Delete** - Built-in soft delete support with global query filters

### Data Access

- ✅ **Hybrid Dapper Approach** - Direct mapping for reads (3-5x faster), shared helpers for writes
- ✅ **Read/Write Separation** - Separate connection factories for reads and writes
- ✅ **DbUp Migrations** - SQL-based migrations with automatic execution
- ✅ **Parameterized Queries** - SQL injection protection with explicit parameterization
- ✅ **Zero Reflection Overhead** - Peak performance with compile-time type safety

### API Features

- ✅ **API Versioning** - URL-based and header-based versioning
- ✅ **OpenAPI/Scalar UI** - Interactive API documentation
- ✅ **Response Compression** - Automatic response compression
- ✅ **Health Checks** - Kubernetes-ready health endpoints

### Security

- ✅ **JWT Authentication** - Centralized token validation
- ✅ **HTTPS Enforcement** - All HTTP requests blocked
- ✅ **Security Headers** - CSP, X-Content-Type-Options, Referrer-Policy
- ✅ **Rate Limiting** - Global, strict, and permissive policies
- ✅ **Input Validation** - FluentValidation with automatic validation

### Performance & Reliability

- ✅ **Caching** - HybridCache with stampede protection
- ✅ **Resilience Patterns** - Retry, circuit breaker, timeout
- ✅ **Connection Pooling** - Optimized database connection management
- ✅ **OpenTelemetry** - Tracing, metrics, and logging

### Developer Experience

- ✅ **Security scanning** - Local vulnerability detection (`./scripts/security-audit.ps1`)
- ✅ **Structured Logging** - Consistent log format with correlation IDs
- ✅ **Exception Handling** - Global exception handler with error IDs
- ✅ **Cancellation Tokens** - Proper async cancellation support
- ✅ **XML Documentation** - Comprehensive code documentation

---

## 📖 Complete Documentation Index

### Foundation & Basics

| Topic | Description | Guide |
|-------|-------------|-------|
| **Project Structure** | Clean Architecture layers and organization | [→](docs/architecture/project-structure.md) |
| **Naming Guidelines** | C# naming conventions and best practices | [→](docs/standards/naming-guidelines.md) |
| **XML Comments** | Code documentation standards | [→](docs/standards/xml-comments-guidelines.md) |
| **Database Schema** | Visual ERD and table relationships | [→](docs/architecture/db-schema-diagram.md) |
| **Database Connections** | Connection string configuration and pooling | [→](docs/standards/database-connection-settings-guidelines.md) |
| **DB Migration Guide** | PostgreSQL to SQL Server migration | [→](docs/guides/database-migration-postgresql-to-sqlserver.md) |
| **DbUp Migrations** | SQL-based migrations and seed data | [→](docs/guides/dbup-migrations.md) |
| **Collection Types** | When to use different collection types | [→](docs/standards/collection-types-guidelines.md) |
| **Explicit Types** | Standards for var vs explicit types | [→](docs/standards/explicit-type-usage-guidelines.md) |

### API Development

| Topic | Description | Guide |
|-------|-------------|-------|
| **API Response Formats** | Complete response format reference | [→](docs/standards/api-response-formats.md) |
| **Validation Filter** | Automatic request validation | [→](docs/architecture/validation-filter.md) |
| **Exception Handling** | Global exception middleware | [→](docs/architecture/exception-handling.md) |
| **Response Compression** | Optimize API responses | [→](docs/standards/response-compression-guidelines.md) |
| **API Versioning** | Version management strategies | [→](docs/standards/api-versioning-guidelines.md) |
| **Scalar UI** | Interactive API documentation | [→](docs/guides/scalar-ui.md) |

### Security

| Topic | Description | Guide |
|-------|-------------|-------|
| **JWT Authentication** | Token-based authentication | [→](docs/architecture/jwt-authentication-filter.md) |
| **ASM Authorization** | Permission-based access control | [→](docs/architecture/asm-authorization.md) |
| **CORS** | Cross-origin resource sharing | [→](docs/architecture/cors.md) |
| **AllowedHosts** | Host header validation | [→](docs/architecture/allowed-hosts.md) |
| **Content-Security-Policy** | CSP headers for security | [→](docs/architecture/content-security-policy.md) |
| **Security Comparison** | Understanding security configurations | [→](docs/architecture/security-configuration-comparison.md) |

### Performance & Optimization

| Topic | Description | Guide |
|-------|-------------|-------|
| **Dapper Hybrid Approach** | High-performance data access (3-5x faster) | [→](docs/architecture/dapper-hybrid-approach.md) |
| **EF Core Migration** | Migrating from Dapper to EF Core (performance & security) | [→](docs/guides/efcore-migration-guide.md) |
| **Performance Guide** | Query optimization and connection pooling | [→](docs/guides/performance-optimization-guide.md) |
| **Dapper Testing** | Testing with mocked connections | [→](docs/testing/testing-comprehensive-guide.md#dapper-repository-testing) |
| **Caching** | Multi-tier caching with stampede protection | [→](docs/guides/hybrid-caching.md) |
| **Response Caching** | HTTP caching with Cache-Control | [→](docs/guides/response-caching-implementation.md) |
| **HttpClient Factory** | Resilient HTTP client configuration | [→](docs/guides/httpclient-factory.md) |
| **HttpClient Lifecycle** | HttpClient disposal and best practices | [→](docs/guides/httpclient-factory.md#deep-dive-lifecycle--connection-pooling) |

### Advanced Patterns

| Topic | Description | Guide |
|-------|-------------|-------|
| **Resilience Patterns** | Retry, circuit breaker, timeout | [→](docs/architecture/resilience.md) |
| **Rate Limiting** | API protection strategies | [→](docs/architecture/rate-limiting.md) |
| **OpenTelemetry Integration** | Distributed tracing, metrics, and logging | [→](docs/architecture/opentelemetry-integration.md) |
| **Correlation ID** | TraceId as X-Correlation-Id for request tracking | [→](docs/architecture/correlation-id.md) |
| **Health Checks** | Kubernetes-ready monitoring | [→](docs/architecture/health-checks.md) |
| **Logging Strategy** | Structured logging best practices | [→](docs/standards/logging-strategy-recommendations.md) |
| **Cancellation Tokens** | Async cancellation patterns | [→](docs/standards/cancellation-token-guidelines.md) |
| **Idempotency** | Idempotent operation patterns | [→](docs/architecture/idempotency-analysis.md) |

### Testing

| Topic | Description | Guide |
|-------|-------------|-------|
| **Testing Comprehensive Guide** | Complete testing strategy, standards, implementation patterns, and Dapper testing | [→](docs/testing/testing-comprehensive-guide.md) |
| **Test Categorization Audit Plan** | Guidelines for categorizing tests (Unit/Integration/E2E) | [→](docs/archive/test-categorization-audit-plan.md) |
| **Test Codebase Comprehensive Audit** | Complete audit report of all 64 test files with compliance verification | [→](docs/archive/test-codebase-comprehensive-audit.md) |

---

## 🛠️ Getting Started

### Configuration Hierarchy

The application uses a configuration hierarchy (later sources override earlier ones):

1. **Base Configuration**: `appsettings.json` - Default settings
2. **Environment-Specific**: `appsettings.{Environment}.json` - Environment overrides (e.g., `appsettings.Development.json`)
3. **Environment Variables**: Highest priority - For secrets and runtime overrides

**Example:** Set environment variable to override configuration:

```bash
# Linux/macOS
export AppSettings__Environment=Production

# Windows (PowerShell)
$env:AppSettings__Environment="Production"
```

**Note:** Nested configuration keys use double underscore (`__`) in environment variables (e.g., `AppSettings__Environment`).

### Configure Database Connection

Update `src/WebShop.Api/appsettings.json`:

```json
{
  "DbConnectionSettings": {
    "Read": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "postgres",
      "Password": "your-password"
    },
    "Write": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "postgres",
      "Password": "your-password"
    }
  }
}
```

**⚠️ Security Note:** Never commit passwords to source control. Use:

- **Development**: [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- **Production**: Hashicorp Vault

### Database Migrations

Database migrations are managed through **DbUp** and run automatically on application startup.

**Migration Location:** `src/WebShop.Api/DbUpMigration/Migrations/`

**To disable automatic migrations:**

```json
{
  "AppSettings": {
    "EnableDatabaseMigration": false
  }
}
```

**Migration Execution:**

- Migrations run automatically when `EnableDatabaseMigration` is `true`
- Only new migrations are executed (idempotent)
- Migrations are executed in alphabetical order
- See [DbUp Migrations Guide](docs/guides/dbup-migrations.md) for details

---

## 🔌 API Endpoints

### API Versioning

- **URL Segment**: `/api/v1/customers` (recommended)
- **HTTP Header**: `api-version: 1`

Default version is `1` if not specified.

### Available Controllers (v1)

**Core Resources:**

- `/api/v1/customers` - Customer management
- `/api/v1/addresses` - Address management
- `/api/v1/products` - Product catalog
- `/api/v1/articles` - Article management
- `/api/v1/orders` - Order management
- `/api/v1/orderpositions` - Order position management
- `/api/v1/stock` - Stock management

**Reference Data:**

- `/api/v1/labels` - Label lookup
- `/api/v1/colors` - Color lookup
- `/api/v1/sizes` - Size lookup

**External Services:**

- `/api/v1/sso` - Single Sign-On operations
- `/api/v1/mis` - Management Information System
- `/api/v1/asm` - Application Security Management

**Management:**

- `/api/v1/cache` - Cache management

### Health Checks

- `GET /health` - Overall health (API + database)
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe
- `GET /health/db` - Database health check

---

## 🔒 Security Checklist

Before deploying to production:

- [ ] **Remove all secrets** from configuration files
- [ ] **Use User Secrets** (development) or **Key Vault** (production)
- [ ] **Configure CORS** with specific origins (no wildcards)
- [ ] **Set AllowedHosts** to specific hostnames (not `*`)
- [ ] **Enable rate limiting** with appropriate limits
- [ ] **Review security headers** configuration
- [ ] **Configure production logging** (appropriate log levels)
- [ ] **Set up monitoring**

[See security configuration comparison →](docs/architecture/security-configuration-comparison.md)

---

## 🏭 Production Readiness

### Critical Items

- ⚠️ **Secrets Management** - Move all secrets to secure storage
- ⚠️ **Monitoring** - Set up application monitoring and alerting (with help of Infra Team)

### Completed Features

- ✅ Production configuration structure
- ✅ Rate limiting implementation
- ✅ Health checks configuration
- ✅ Exception handling middleware
- ✅ Database connection pooling
- ✅ Performance optimizations

---

## 🧪 Development

### Building the Solution

```bash
# Build the entire solution
dotnet build

# Build a specific project
dotnet build src/WebShop.Api/WebShop.Api.csproj

# Build with no restore (faster for subsequent builds)
dotnet build --no-restore
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/WebShop.Business.Tests
```

**Using the coverage script (recommended):**

```powershell
# Summary report with coverage results
pwsh scripts/run-coverage.ps1

# Detailed coverage analysis
pwsh scripts/run-coverage.ps1 -ReportType Detailed

# HTML coverage report (requires dotnet-reportgenerator-globaltool)
pwsh scripts/run-coverage.ps1 -ReportType Html
```

See the [Testing Comprehensive Guide](docs/testing/testing-comprehensive-guide.md) for testing standards, patterns, and best practices.

### Security Scanning

Run local security checks before pushing code to catch common vulnerabilities early:

```powershell
pwsh scripts/security-audit.ps1 -Fast    # Quick check - NuGet vulnerabilities only (~30 seconds)
pwsh scripts/security-audit.ps1          # Full audit - Includes credentials, secrets, and NuGet scan (~1-2 minutes)
pwsh scripts/security-audit.ps1 -Help    # See all options
```

**Optional: Install Enhanced Security Tools**

Install optional tools for more comprehensive scanning:

```powershell
pwsh scripts/install-security-tools.ps1        # Interactive installer
pwsh scripts/install-security-tools.ps1 -All   # Install all without prompting
```

Optional tools:

- **git-secrets** - Scans git history for API keys, passwords, and tokens

**Built-in scanning (always available):**

- NuGet vulnerability detection (`dotnet list package --vulnerable`)
- Hardcoded credentials detection

**Prerequisite for PowerShell:** [PowerShell Core (pwsh)](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell)

- Windows: Pre-installed with Windows PowerShell, or install [PowerShell Core](https://github.com/PowerShell/PowerShell)
- macOS: `brew install powershell`
- Linux: See [official installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

For complete details, see [Security Scanning Guide](docs/standards/security-scanning.md).

### Code Quality Analysis

Run **SonarQube SAST analysis** locally to catch code quality issues, security vulnerabilities, and technical debt before committing:

```powershell
pwsh scripts/sonarqube-scan.ps1    # Run analysis with SonarCloud
pwsh scripts/sonarqube-scan.ps1 -Help # See configuration options
```

**Quick Setup:**

1. Set your SonarCloud token:

   ```powershell
   $env:SONAR_TOKEN = "your-sonarcloud-token"
   ```

2. Run the analysis - it automatically uses the WebShop project configuration

**Configuration Details:** The project uses `sonar-config.json` for SonarQube settings:

- **Project Key:** `86f3bb56-974d-4a8f-becd-6e4a030e5d4a_webshop`
- **Organization:** `86f3bb56-974d-4a8f-becd-6e4a030e5d4a`
- **Language:** C#
- **Scope:** Analyzes `src/` directory, excludes test projects and build artifacts

For complete setup and troubleshooting, see [SonarQube SAST Setup Guide](docs/guides/sonarqube-setup-guide.md).

### Adding NuGet Packages

This project uses **Central Package Management (CPM)** for consistent package versions across all projects.

**Steps:**

1. **Add package version** to `Directory.Packages.props`:

   ```xml
   <PackageVersion Include="PackageName" Version="1.0.0" />
   ```

2. **Reference in project file** (no version needed):

   ```xml
   <PackageReference Include="PackageName" />
   ```

**Benefits:**

- Single source of truth for package versions
- Easier dependency management
- Prevents version conflicts

### Project Dependencies

```
WebShop.Api
  ├── WebShop.Business
  ├── WebShop.Infrastructure
  └── WebShop.Util

WebShop.Business
  └── WebShop.Core

WebShop.Infrastructure
  └── WebShop.Core

WebShop.Core
  └── (No dependencies)

WebShop.Util
  └── (No dependencies)
```

---

## 📚 How to Use This Boilerplate

### For New Projects

1. **Clone this repository** as your starting point
2. **Rename projects** to match your domain (e.g., `YourApp.Api`, `YourApp.Business`)
3. **Update namespaces** throughout the solution
4. **Customize entities** in `WebShop.Core/Entities/`
5. **Follow the learning path** above to understand each component
6. **Reference documentation** as you implement features

### For Learning

1. **Start with Level 1** (Foundation & Basics)
2. **Read the guides** in order of complexity
3. **Study the code** alongside the documentation
4. **Experiment** with the codebase and database
5. **Progress to advanced topics** as you gain confidence

### For Reference

- Use the **Documentation Index** above to find specific topics
- Each guide includes **Why**, **How**, **Benefits**, and **Examples**
- Code examples are provided in each guide
- Best practices are documented throughout

---

## 🤝 Contributing

This is a boilerplate solution. When using it:

1. **Fork or clone** for your project
2. **Customize** to your needs
3. **Follow the patterns** established in the codebase
4. **Reference the documentation** for guidance

---

## 📄 License

This project is licensed under the MIT License.

---

## 🎯 Quick Reference

**Common Tasks** | **Guide**
---|---
Understand project structure | [Project Structure](docs/architecture/project-structure.md)
Set up database | [DbUp Migrations](docs/guides/dbup-migrations.md)
Optimize data access | [Dapper Hybrid Approach](docs/architecture/dapper-hybrid-approach.md)
Add validation | [Validation Filter](docs/architecture/validation-filter.md)
Handle errors | [Exception Handling](docs/architecture/exception-handling.md)
Understand responses | [API Response Formats](docs/standards/api-response-formats.md)
Secure API | [Security Configuration](docs/architecture/security-configuration-comparison.md)
Configure ASM auth | [ASM Authorization](docs/architecture/asm-authorization.md)
Optimize performance | [Performance Guide](docs/guides/performance-optimization-guide.md)
Run local security scan | [Security Scanning](docs/standards/security-scanning.md)
Analyze code quality | [SonarQube SAST Setup](docs/guides/sonarqube-setup-guide.md)
Test repositories | [Testing Guide](docs/testing/testing-comprehensive-guide.md#dapper-repository-testing)
Add caching | [Caching Guide](docs/guides/hybrid-caching.md)
Configure CORS | [CORS Guide](docs/architecture/cors.md)
Version API | [API Versioning](docs/standards/api-versioning-guidelines.md)
Configure logging | [Logging Strategy](docs/standards/logging-strategy-recommendations.md)
Add observability | [OpenTelemetry Integration](docs/architecture/opentelemetry-integration.md)
Add health checks | [Health Checks](docs/architecture/health-checks.md)
Write unit tests | [Testing Guide](docs/testing/testing-comprehensive-guide.md#implementation-patterns)

---

## ❓ Troubleshooting

### Common Issues

**Database connection fails:**

- Verify PostgreSQL is running: `pg_isready` or check service status
- Check connection string in `appsettings.json`
- Verify database exists: `psql -l` lists all databases
- Check firewall/network settings

**Migrations not running:**

- Verify `EnableDatabaseMigration` is `true` in `appsettings.json`
- Check application logs for migration errors
- Ensure database user has CREATE/ALTER permissions

**Port already in use:**

- Change port in `Properties/launchSettings.json`
- Or kill the process using the port: `lsof -ti:7109 | xargs kill` (macOS/Linux)

**Configuration not loading:**

- Verify `appsettings.json` syntax is valid JSON
- Check environment variable names use double underscore (`__`) for nested keys
- Review configuration hierarchy in [Configuration Hierarchy](#configuration-hierarchy)

**Scalar UI not accessible:**

- Verify `AppSettings:Environment` is NOT "Production"
- Check `appsettings.Development.json` has correct environment setting
- Ensure OpenAPI endpoints are configured

For more help, see the relevant documentation guides or check application logs.

---

**Happy Coding! 🚀**
