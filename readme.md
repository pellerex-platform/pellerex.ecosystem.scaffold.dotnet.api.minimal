# .NET API Scaffold: Minimal Cloud-native API
*By [Pellerex.com](https://pellerex.com)*

A production-ready, minimal .NET API template with Docker containerization, health monitoring, logging, and secret management built-in.

## Features

- 🐳 **Docker Ready** - Containerized with multi-stage builds
- 🔍 **Health Checks** - Live, ready, and startup endpoints
- 📊 **Monitoring** - Structured logging with Application Insights
- 🔐 **Secret Management** - User secrets integration
- 📚 **API Documentation** - Swagger/OpenAPI built-in
- 🌐 **CORS & Compression** - Production configurations
- ✅ **Validation** - FluentValidation integration

*Made with ❤️ by [Pellerex.com](https://pellerex.com)*

## 🚀 Quick Start

### 1. **First Time Setup**
```bash
./start/setup-secrets.sh
```
- Creates user secrets directory
- Sets up default database connection string
- **Run this once** when setting up the project

### 2. **Local Development** (Recommended)
```bash
./start/run-local.sh
```
- Runs with `dotnet watch` for hot reload
- Uses user secrets for configuration
- **Fastest for development**

### 3. **Docker Testing**
```bash
./start/run-docker.sh
```
- Builds Docker image
- Runs container with volume-mounted secrets
- **Tests containerized deployment**

## 🔐 Secrets Management

**User Secrets Location:**
```
~/.microsoft/usersecrets/<marketplace-product-id>/secrets.json
```

### 📝 **Important: Database Host Configuration**

**For Local Development:**
```json
{
  "DbConnectionString": "Server=.;Database=MinimalDev;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;"
}
```

**For Docker:**
```json
{
  "DbConnectionString": "Server=host.docker.internal;Database=MinimalDev;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;"
}
```

💡 **Note:** Change `Server=.` to `Server=host.docker.internal` when running in Docker to connect to your local database from the container.

## 🎯 Available Endpoints

- **Local Development:** http://localhost:8890
- **Docker:** http://localhost:8890
- **Swagger:** `/swagger`
- **Health Checks:** `/health/live`, `/health/ready`, `/health/startup`

## Unit Tests

`unit-tests/` holds the app's unit tests, written with TUnit like the acceptance tests. They start the app in memory, with no database, network or container; run them with `dotnet run --project unit-tests/RepoUniquePascalIdentifier.UnitTests.csproj`. Every build runs them in the Dockerfile's `unit-tests` stage before it builds the image, keeps their results and line coverage as the `test-coverage` artifact, which Pellerex reads for the app's Test Coverage tab, and stops when a test fails.

## Acceptance Tests

`acceptance-tests/` holds the app's acceptance tests: black-box tests that call the running app over HTTP, as any client does, and never load its code.

- **The machinery is one package.** `Pellerex.Testing`, Pellerex's testing library for .NET, holds in its acceptance part the client the tests call the app with, set-up and tear-down, the checks every run makes, and the adapter that writes the result file. `AcceptanceTests/AcceptanceTests.csproj` pins its version, so moving to a new version changes that one number. A test file takes it with `using Pellerex.Testing.Acceptance.Run;` and `using static Pellerex.Testing.Acceptance.ClientHelper.AcceptanceConstants;`. The package's code and its own tests live in the `pellerex.ecosystem.sdk.testing.dotnet` repository.
- **Every test has a type.** Mark it `[Property(TestTypes.PropertyName, TestTypes.Release)]`, `[Property(TestTypes.PropertyName, TestTypes.Acceptance)]`, or both. Release tests are the only ones that ever run against a deployed copy, so they must be safe on live: light, and touching only data they create. Acceptance tests run only against a local copy. A test with no type makes the run an error.
- **Every test has a name of its own.** The result file keeps a test by its name alone, so two tests with one name, even in different classes, make the run an error.
- **Run a set** against a running copy of the app:
  ```bash
  acceptance-tests/run.sh --type Acceptance --target http://localhost:<port-number> --out result.json
  ```
  It ends with 0 when every test passed, 1 when a test failed, and 2 when the run itself broke. The result file holds every test with every request it sent and every response it got, with secrets taken out. It restores the pack's packages first, since the adapter comes in the package; when they cannot be restored, the run ends with 2 and the restore's errors, and no result file.
- **Call the app** with `TestRun.Client()`, using paths without a leading slash, such as `v1/hello`. When the run has a credential, a folder of files named in `ACCEPTANCE_TESTS_CREDENTIALS`, the client sends it.
- **Set-up and tear-down.** Name each record a test creates with `TestData.Name("kind")`, create it through the app's API, and register how to remove it with `TestData.RemoveAfterTest(name, client => client.DeleteAsync(...))`. Tear-down removes it after the test, even when the test fails.
- **Examples** are in `acceptance-tests/AcceptanceTests/Examples`: one Release test, one Acceptance test, and one of both.
- **`standup.yaml`** tells the stand-up tool how to start this app in a container for a local run.

## 📋 What's Included

### 🏗️ Architecture
- **Minimal API Structure**: Streamlined `api/` folder with essential components
- **Docker**: Multi-stage build with health checks
- **Swagger**: API documentation at `/swagger`
- **Health Checks**: `/health/live`, `/health/ready`, `/health/startup`

### � Features
- CORS configuration and response compression
- Request validation with FluentValidation  
- API versioning and secret management
- Application Insights integration
- Automated logging and exception handling
- User secrets for development
- **User Secrets** - Development-time secret management
- **Environment Variables** - Runtime configuration
- **appsettings.json** - Environment-specific settings

### 🔍 **Observability**
- **Serilog** - Structured logging
- **Application Insights** - Cloud monitoring integration
- **Request Tracing** - Automatic HTTP request logging

## About Pellerex.com

![Pellerex.com](https://pellerex.com/favicon.ico)

This scaffold is part of the **Pellerex Cloud-Native Platform** — an enterprise-grade system designed to help teams deliver secure, compliant, and production-ready applications at speed.

Pellerex combines a **self-service Internal Developer Platform (IDP)** with a **governance-driven PaaS engine**, giving you everything you need to build, ship, and run software confidently.

### What Pellerex Offers
- ⚡ **Faster Delivery** — Ship APIs, apps, and infrastructure in minutes, not weeks.  
- 🛡️ **Enterprise Security & Compliance** — Identity-aware routing, audit logging, policy enforcement, and zero-trust by design.  
- 🔐 **Secrets & Data** — Automated provisioning of databases and secure storage in Key Vaults.  
- 🔍 **Observability Built-In** — Logs, metrics, and health checks integrated with Application Insights.  
- 🌍 **Multi-Cloud Ready** — Deploy seamlessly across Azure, AWS, and Kubernetes.  
- 🧩 **Extensible by Design** — Scaffolds, CI/CD pipelines, and plugins for .NET, Node.js, Python, and more.  

Pellerex enables enterprises to **balance speed with governance**, ensuring that developers can move fast while organizations stay compliant and secure.

**Ship it in minutes. Feel the impact.**  
Learn more at [**pellerex.com**](https://pellerex.com).
