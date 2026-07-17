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
