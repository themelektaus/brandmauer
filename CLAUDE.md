# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Brandmauer is a .NET 9 web application that functions as a reverse proxy, firewall, and network management system with certificate management, DNS capabilities, and monitoring features. It's designed to run on both Linux (production) and Windows (development/debug).

## Build & Run Commands

### Development (Windows)
```bash
dotnet build
dotnet run
```
The application runs on `http://localhost:5080` and `https://localhost:5443` in development mode.

### Production Build (Linux)
```bash
dotnet publish -c Linux -p:PublishProfile=Properties/PublishProfiles/Linux.pubxml
```
This creates a self-contained, single-file Linux executable for `linux-x64` runtime.

### Production Build (Windows)
```bash
dotnet publish -c Windows -p:PublishProfile=Properties/PublishProfiles/Windows.pubxml
```

Note: On Linux production builds, the application listens on standard HTTP (80) and HTTPS (443) ports. Windows builds use ports 5080 and 5443.

## Architecture Overview

### Core Components

**Database System** (`Database.cs`)
- JSON-based persistence stored in `Data/Database.json`
- Thread-safe access via `ThreadsafeContext`
- All CRUD operations use `Database.Use()` or `Database.UseAsync()` methods
- Models auto-register/unregister themselves with the database
- Supports hot-reload via file watching

**Model System** (`Models/Model.cs`)
- Base class for all data entities
- Auto-generates unique identifiers
- Lifecycle managed through `IDisposable` pattern
- Models can implement `IOnDeserialize` for post-load initialization
- Models can implement `IAsyncUpdateable` for periodic updates

**Middleware Pipeline** (`Program.cs:82-98`)
Middlewares are executed in this specific order:
1. `HelloWorldMiddleware` (DEBUG only)
2. `WellKnownMiddleware` - Serves `.well-known` paths for ACME/Let's Encrypt
3. `LoginMiddleware` - Handles authentication
4. `WhitelistMiddleware` - IP/domain whitelisting and permission requests
5. `PushMiddleware` - Server-sent events
6. `ShareMiddleware` - File sharing
7. `IconMiddleware` - Dynamic icon serving
8. `ReverseProxyPreparatorMiddleware` - Prepares reverse proxy context
9. `YarpReverseProxyMiddleware` - YARP-based reverse proxy
10. `CustomReverseProxyMiddleware` - Custom reverse proxy implementation
11. `LiveCodeMiddleware` - Dynamic C# code execution
12. `FrontendMiddleware` - Serves the web frontend
13. `TeapotMiddleware` - Fallback 418 response

**Endpoint System** (`Endpoints/Endpoint.cs`)
- RESTful API endpoints mapped via `Endpoint.MapAll()`
- Endpoints are split across multiple partial classes by feature area
- All API routes use `/api` prefix
- Platform-specific endpoints controlled via `#if LINUX` / `#if DEBUG` directives

**Background Tasks** (`Tasks/IntervalTask.cs`)
- Base class for all background operations
- Controlled via attributes: `[Interval]`, `[Delay]`, `[OneShot]`
- Tasks registered in `Program.cs:100-114`
- All tasks implement graceful shutdown via `IAsyncDisposable`

### Key Feature Areas

**Reverse Proxy** (`Models/ReverseProxyRoute.cs`, `Middlewares/*ReverseProxy*.cs`)
- Two implementations: YARP (Microsoft's reverse proxy) and custom
- Routes stored in database with dynamic script support
- Support for SSL/TLS termination with automatic certificate selection
- Features `ReverseProxyFeature` for middleware communication

**Certificate Management** (`Models/Certificate.cs`, `Helpers/CertificateUtils.cs`)
- Automatic certificate authority (CA) generation on first run
- Let's Encrypt integration via Certes library
- Certificate renewal via `IntervalTask_RenewCertifcates`
- PFX format for storage, multiple export formats supported

**Authentication** (`Models/Authentication.cs`, `Middlewares/LoginMiddleware.cs`)
- TOTP-based two-factor authentication (`Helpers/TotpUtils.cs`)
- Session management via tokens
- Permission system with email notifications (MJML templates)

**Network Features**
- NAT routing (`Models/NatRoute.cs`)
- Dynamic DNS (`Models/DynamicDnsHost.cs`)
- DNS server (`IntervalTask_DnsServer`)
- IP filtering and whitelisting
- Linux iptables management (`Classes/IpTables*.cs`)

**Monitoring** (`Models/Monitor*.cs`)
- Multiple monitor types: Process, Service, SQL Connection
- Configured via polymorphic model system
- Status tracking and alerts

**Live Code Execution** (`Middlewares/LiveCodeMiddleware.cs`)
- Executes C# code dynamically using Roslyn
- Access to full application context
- Used for debugging and dynamic operations

## Configuration Files

- `Data/Database.json` - Main database file (auto-created)
- `appsettings.json` / `appsettings.Development.json` - ASP.NET configuration
- `permission-request.mjml` - Email template for permission requests
- `brandmauer.service` - systemd service file for Linux deployment
- `brandmauer-control` - Shell script for service management

## Platform-Specific Behavior

Code uses conditional compilation:
- `#if LINUX` - Production Linux features (iptables, systemd, standard ports)
- `#if WINDOWS` - Windows-specific features (Windows Services support)
- `#if DEBUG` - Development features (verbose logging, HelloWorld endpoint)
- `#if FORTI` - FortiClient VPN integration (optional)

## Important Patterns

1. **Thread Safety**: All database access must use `Database.Use()` or `Database.UseAsync()`
2. **Model Lifecycle**: Models register themselves on creation and must be disposed properly
3. **Logging**: Use `Audit` class for application logging, not `Console.WriteLine`
4. **Unsafe Code**: Project allows unsafe blocks for low-level operations
5. **SSL/TLS**: Certificate selection happens dynamically via `ServerCertificateSelector` in `Program.cs:50-61`

## Common Tasks

### Adding a New Model
1. Create class inheriting from `Model` in `Models/`
2. Add collection property to `Database` class
3. Create endpoint partial class in `Endpoints/Endpoint.YourModel.cs`
4. Map CRUD endpoints in `Endpoint.MapAll()`
5. Implement `IOnDeserialize` if post-load initialization is needed

### Adding a Background Task
1. Create class inheriting from `IntervalTask` in `Tasks/`
2. Add `[Interval(seconds)]` attribute for periodic execution
3. Implement `OnStartAsync()`, `OnBeforeFirstTickAsync()`, `OnTickAsync()`, `OnDisposeAsync()`
4. Register in `Program.cs` middleware registration section

### Adding a Middleware
1. Create middleware class in `Middlewares/`
2. Implement `Invoke(HttpContext context)` method with `RequestDelegate next` constructor parameter
3. Add to middleware pipeline in `Program.cs:82-98` (order matters!)

## Dependencies of Note

- **YARP** - Microsoft's reverse proxy library
- **Certes** - ACME/Let's Encrypt client
- **SSH.NET** - SSH client operations
- **LibSassHost** - SCSS compilation
- **MailKit** - Email sending
- **Otp.NET** - TOTP generation
- **QRCoder** - QR code generation for 2FA setup
- **Microsoft.CodeAnalysis.CSharp** - Roslyn for live code execution
