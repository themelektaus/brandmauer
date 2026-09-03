# Brandmauer - Network Management and Reverse Proxy System

[![Build Status](https://github.com/themelektaus/brandmauer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/themelektaus/brandmauer/actions/workflows/dotnet.yml)

**Version:** 0.3.1.4
**Framework:** .NET 9.0
**Platform:** Cross-platform (Linux production, Windows development)

## Project Overview

Brandmauer is an enterprise-grade, all-in-one network management solution that combines the functionality of a reverse proxy, firewall, certificate authority, and network monitoring system into a single, lightweight application. Built on .NET 9, it provides comprehensive network infrastructure management with an emphasis on security, automation, and ease of deployment.

The name "Brandmauer" (German for "firewall") reflects the application's core mission: to serve as a protective barrier and intelligent traffic manager for network infrastructure. Unlike traditional solutions that require multiple disparate tools, Brandmauer consolidates essential network services into one cohesive platform.

### Core Value Proposition

- **Unified Network Management**: Single application for reverse proxy, firewall, DNS, NAT routing, and certificate management
- **Zero-Configuration SSL/TLS**: Automatic certificate generation and renewal via Let's Encrypt integration
- **Dynamic Traffic Control**: Flexible routing with support for custom C# scripting
- **Security-First Design**: Built-in authentication, IP whitelisting, and permission request system
- **Production-Ready**: Designed for Linux production environments with systemd integration
- **Developer-Friendly**: Full Windows development support with hot-reload capabilities

## Key Features and Capabilities

### 1. Reverse Proxy

Brandmauer offers dual reverse proxy implementations for maximum flexibility:

- **YARP Integration**: Leverages Microsoft's Yet Another Reverse Proxy (YARP) for high-performance proxying
- **Custom Implementation**: Proprietary reverse proxy engine with advanced features
- **Dynamic Routing**: Configure routes through web interface or API with live updates
- **Script Support**: Inject custom C# code for request/response manipulation
- **SSL/TLS Termination**: Automatic certificate selection based on Server Name Indication (SNI)
- **Header Manipulation**: Full control over request and response headers
- **WebSocket Support**: Seamless proxying of WebSocket connections

### 2. Certificate Management

Comprehensive SSL/TLS certificate lifecycle management:

- **Automatic CA Generation**: Creates self-signed Certificate Authority on first run
- **Let's Encrypt Integration**: Automated certificate issuance via ACME protocol (using Certes library)
- **Auto-Renewal**: Background task monitors and renews certificates before expiration
- **Multiple Export Formats**: PFX, PEM, CER, and other formats supported
- **Certificate Storage**: Secure storage with hot-reload capability
- **SNI-Based Selection**: Dynamic certificate serving based on requested hostname

### 3. Network Security and Firewall

Multi-layered security features protect your infrastructure:

- **IP Whitelisting**: Granular access control based on IP addresses and ranges
- **Domain Whitelisting**: Host-based access restrictions
- **Permission Request System**: Users can request access with email notifications (MJML templates)
- **Linux iptables Integration**: Native firewall rule management on Linux systems
- **NAT Routing**: Port forwarding and network address translation
- **Rule-Based Access Control**: Flexible permission system with fine-grained controls

### 4. Authentication System

Enterprise-grade authentication with modern security standards:

- **TOTP Two-Factor Authentication**: Time-based One-Time Password implementation
- **QR Code Generation**: Easy 2FA setup via QR codes
- **Session Management**: Token-based authentication with secure session handling
- **Permission System**: Role-based access control
- **Email Notifications**: Automated alerts for permission requests and security events

### 5. DNS Services

Built-in DNS server functionality:

- **Dynamic DNS (DDNS)**: Automatic hostname updates for dynamic IP addresses
- **Custom DNS Server**: Full DNS server implementation for internal name resolution
- **DNS Record Management**: Configure A, AAAA, CNAME, and other record types
- **Integration with Routing**: DNS records automatically sync with reverse proxy routes

### 6. Monitoring and Observability

Real-time monitoring of critical services and infrastructure:

- **Multiple Monitor Types**:
  - Process Monitoring: Track application and service processes
  - Service Monitoring: Monitor Windows/Linux services
  - SQL Connection Monitoring: Database connectivity checks
- **Status Tracking**: Real-time status updates and historical data
- **Alert System**: Configurable alerts for failures and anomalies
- **Server-Sent Events (SSE)**: Push notifications for real-time updates

### 7. File Sharing

Secure file sharing capabilities integrated into the platform:

- **Authenticated Sharing**: Share files with access controls
- **Dynamic Serving**: Files served through middleware with permission checks
- **Integration with Authentication**: Leverages existing auth system for security

### 8. Live Code Execution

Powerful debugging and automation capability:

- **Dynamic C# Execution**: Run C# code on-the-fly using Roslyn compiler
- **Full Context Access**: Access to entire application context and database
- **Development Tool**: Ideal for debugging, testing, and rapid prototyping
- **API Endpoint**: Execute code via HTTP requests for automation

### 9. Cross-Platform Support

Optimized for different deployment scenarios:

- **Linux Production**: Standard ports (80/443), systemd service, iptables integration
- **Windows Development**: High ports (5080/5443), Visual Studio integration
- **Windows Production**: Windows Service support via Microsoft.Extensions.Hosting.WindowsServices
- **Conditional Compilation**: Platform-specific features cleanly separated via preprocessor directives

## Technical Architecture

### Technology Stack

#### Core Framework
- **.NET 9.0**: Latest .NET framework for high performance and modern features
- **ASP.NET Core**: Web framework and hosting infrastructure
- **Kestrel**: High-performance web server with custom SSL/TLS configuration

#### Key Libraries and Dependencies

**Network and Proxy**
- **YARP (Yet Another Reverse Proxy) 2.3.0**: Microsoft's reverse proxy library
- **DNS 7.0.0**: DNS protocol implementation
- **IPAddressRange 6.0.0**: IP address range parsing and matching

**Security and Certificates**
- **Certes 3.0.4**: ACME protocol client for Let's Encrypt
- **Portable.BouncyCastle 1.9.0**: Cryptographic operations
- **System.Formats.Asn1 9.0.6**: ASN.1 encoding for certificates
- **Otp.NET 1.4.0**: TOTP two-factor authentication
- **QRCoder 1.6.0**: QR code generation for 2FA setup

**Data and Communication**
- **MailKit 4.13.0**: Email sending via SMTP
- **Mjml.NET 4.9.0**: Email template rendering (MJML to HTML)
- **Microsoft.Data.SqlClient 6.0.2**: SQL Server connectivity for monitoring
- **SSH.NET 2025.0.0**: SSH client for remote operations

**Development and Compilation**
- **Microsoft.CodeAnalysis.CSharp 4.14.0**: Roslyn compiler for live code execution
- **LibSassHost 1.5.0**: SCSS/Sass compilation for frontend styling

**UI**
- **Material.Icons 2.4.1**: Material Design icons for web interface

### System Architecture

#### Database System

Brandmauer uses a custom JSON-based persistence layer designed for simplicity and reliability:

- **File-Based Storage**: All data stored in `Data/Database.json`
- **Thread-Safe Operations**: All database access protected by `ThreadsafeContext`
- **Automatic Serialization**: Models automatically register/unregister themselves
- **Hot-Reload Support**: File watching enables live configuration updates
- **CRUD API**: Consistent `Database.Use()` and `Database.UseAsync()` patterns

```csharp
// Example database usage pattern
Database.UseAsync(async db => {
    var route = new ReverseProxyRoute {
        Domain = "example.com",
        Target = "http://localhost:3000"
    };
    db.ReverseProxyRoutes.Add(route);
});
```

#### Model System

Base entity framework with lifecycle management:

- **Inheritance-Based**: All entities inherit from `Model` base class
- **Auto-ID Generation**: Unique identifiers automatically assigned
- **IDisposable Pattern**: Proper resource cleanup and deregistration
- **Deserialization Hooks**: `IOnDeserialize` interface for post-load initialization
- **Update Scheduling**: `IAsyncUpdateable` interface for periodic updates

#### Middleware Pipeline

ASP.NET Core middleware execute in precise order for optimal performance and security:

1. **HelloWorldMiddleware** (Debug only): Development testing endpoint
2. **WellKnownMiddleware**: Serves `.well-known` paths for ACME challenges
3. **LoginMiddleware**: Authentication and session management
4. **WhitelistMiddleware**: IP/domain filtering and permission requests
5. **PushMiddleware**: Server-Sent Events for real-time notifications
6. **ShareMiddleware**: Authenticated file sharing
7. **IconMiddleware**: Dynamic icon serving
8. **ReverseProxyPreparatorMiddleware**: Prepares reverse proxy context
9. **YarpReverseProxyMiddleware**: Microsoft YARP reverse proxy
10. **CustomReverseProxyMiddleware**: Custom reverse proxy implementation
11. **LiveCodeMiddleware**: Dynamic C# code execution
12. **FrontendMiddleware**: Serves web interface (HTML/JS/CSS)
13. **TeapotMiddleware**: HTTP 418 fallback response

This ordering ensures security checks occur before proxying, authentication happens early, and the frontend is served only after all other middleware have passed.

#### Background Task System

Automated operations via interval-based task scheduling:

- **Base Class**: `IntervalTask` provides common infrastructure
- **Attribute Configuration**:
  - `[Interval(seconds)]`: Periodic execution interval
  - `[Delay(milliseconds)]`: Initial startup delay
  - `[OneShot]`: Execute once and terminate
- **Graceful Shutdown**: All tasks implement `IAsyncDisposable`
- **Built-in Tasks**:
  - `IntervalTask_Continuously`: High-frequency operations
  - `IntervalTask_Daily`: Daily maintenance tasks
  - `IntervalTask_DnsServer`: DNS service operation
  - `IntervalTask_ReloadDatabase`: Hot-reload of configuration
  - `IntervalTask_RenewCertificates`: Certificate renewal (Linux only)
  - `IntervalTask_UpdateBrandmauer`: Self-update mechanism (Linux only)
  - `IntervalTask_Startup`: Boot-time initialization (Linux only)

#### RESTful API

Comprehensive REST API for all operations:

- **Endpoint System**: Modular endpoint registration via `Endpoint.MapAll()`
- **Partial Classes**: Endpoints organized by feature area in separate files
- **Consistent Routing**: All API routes prefixed with `/api`
- **Platform-Specific**: Conditional endpoints via preprocessor directives
- **JSON Serialization**: Field-based serialization with exception handling

### Security Architecture

Brandmauer implements defense-in-depth security:

1. **Transport Security**: TLS 1.2+ with automatic certificate management
2. **Authentication Layer**: TOTP-based 2FA with session tokens
3. **Authorization Layer**: IP whitelisting and permission system
4. **Network Layer**: iptables integration for packet filtering (Linux)
5. **Application Layer**: Middleware-based request validation
6. **Audit Logging**: Comprehensive logging via `Audit` class

### Data Flow

**Incoming HTTPS Request Flow:**
```
Client Request
    ↓
Kestrel (SSL/TLS termination with SNI-based certificate selection)
    ↓
WellKnownMiddleware (ACME challenges)
    ↓
LoginMiddleware (Authentication check)
    ↓
WhitelistMiddleware (IP/domain filtering)
    ↓
ReverseProxyPreparatorMiddleware (Setup proxy context)
    ↓
YarpReverseProxyMiddleware OR CustomReverseProxyMiddleware
    ↓
Upstream Server (forwarded request)
    ↓
Response flows back through middleware chain
    ↓
Client receives response
```

## Platform-Specific Features

### Linux (Production)

- **Standard Ports**: HTTP on port 80, HTTPS on port 443
- **systemd Integration**: Runs as system service with automatic restart
- **iptables Management**: Native firewall rule manipulation
- **Self-Update**: Automatic application updates via background task
- **Certificate Renewal**: Automated Let's Encrypt certificate renewal
- **Service Management**: Control via `brandmauer-control` shell script

**systemd Service Configuration:**
```ini
[Unit]
Description=Brandmauer
After=network.target

[Service]
Type=simple
Restart=always
RestartSec=5
User=root
WorkingDirectory=/app/brandmauer
ExecStart=/app/brandmauer/Brandmauer

[Install]
WantedBy=multi-user.target
```

### Windows (Development)

- **High Ports**: HTTP on port 5080, HTTPS on port 5443 (no admin rights required)
- **Visual Studio Integration**: Full debugging and IntelliSense support
- **Hot Reload**: Code changes apply without restart
- **Console Logging**: Verbose output for development debugging

### Windows (Production)

- **Windows Service**: Runs as background service via `Microsoft.Extensions.Hosting.WindowsServices`
- **Service Control**: Manageable via Windows Services control panel

## Deployment Information

### Build Configurations

Three build configurations support different scenarios:

1. **Debug**: Development builds with verbose logging and debug endpoints
2. **Linux**: Production build for Linux with platform-specific features
3. **Windows**: Production build for Windows with service support

### Build and Deployment

**Development (Windows):**
```bash
dotnet build
dotnet run
# Access at http://localhost:5080 and https://localhost:5443
```

**Production Build (Linux):**
```bash
dotnet publish -c Linux -p:PublishProfile=Properties/PublishProfiles/Linux.pubxml
```
- Creates self-contained, single-file executable for `linux-x64`
- Includes native dependencies (LibSassHost)
- Post-publish script copies to deployment locations
- Executable runs on standard ports 80/443

**Production Build (Windows):**
```bash
dotnet publish -c Windows -p:PublishProfile=Properties/PublishProfiles/Windows.pubxml
```
- Self-contained Windows executable
- Includes Windows Service hosting support
- Post-publish script manages distribution

### System Requirements

**Linux:**
- Linux kernel with iptables support
- systemd init system
- .NET 9.0 runtime (bundled in self-contained build)
- Root access for port binding (80/443) and iptables
- Network access for Let's Encrypt certificate issuance

**Windows:**
- Windows 10/11 or Windows Server 2016+
- .NET 9.0 runtime (bundled in self-contained build)
- Admin rights for production deployment (service installation)

### Installation

**Linux systemd Service:**
```bash
# Copy executable to /app/brandmauer/
chmod +x /app/brandmauer/Brandmauer

# Copy service file
cp brandmauer.service /etc/systemd/system/

# Enable and start service
systemctl daemon-reload
systemctl enable brandmauer
systemctl start brandmauer

# Check status
systemctl status brandmauer
```

**Windows Service:**
```bash
# Run as administrator
sc create Brandmauer binPath="C:\Path\To\Brandmauer.exe"
sc start Brandmauer
```

### Configuration

All configuration persists in `Data/Database.json`, created automatically on first run. The file contains:

- Reverse proxy routes
- SSL/TLS certificates
- Authentication settings
- Firewall rules and whitelists
- NAT routes
- DNS configurations
- Monitor definitions
- Application settings

**Additional Configuration Files:**
- `appsettings.json`: ASP.NET Core configuration
- `appsettings.Development.json`: Development overrides
- `permission-request.mjml`: Email template for permission requests

## Main Components and Their Roles

### Models (Data Entities)

Located in `Models/` directory:

- **Certificate**: SSL/TLS certificate storage and management
- **ReverseProxyRoute**: Proxy routing configuration
- **Authentication**: User authentication and session data
- **Host**: Managed host definitions
- **Rule**: Firewall and access rules
- **NatRoute**: NAT routing and port forwarding
- **DynamicDnsHost**: Dynamic DNS host configurations
- **Share**: File sharing definitions
- **SmtpConnection**: Email server configurations
- **Monitor**: Base monitoring class with polymorphic implementations:
  - `Monitor.Process`: Process monitoring
  - `Monitor.Service`: Service status monitoring
  - `Monitor.SqlConnection`: Database connectivity monitoring
- **Service**: Service management (Linux/Windows)
- **Config**: Application-wide configuration
- **PushListener**: Server-Sent Events listener registration

### Middlewares (Request Processing)

Located in `Middlewares/` directory:

- **WellKnownMiddleware**: ACME challenge and well-known path serving
- **LoginMiddleware**: Authentication and authorization
- **WhitelistMiddleware**: IP/domain filtering and permission management
- **PushMiddleware**: Server-Sent Events push notification system
- **ShareMiddleware**: Authenticated file serving
- **IconMiddleware**: Dynamic icon generation and serving
- **ReverseProxyPreparatorMiddleware**: Prepares request context for proxying
- **YarpReverseProxyMiddleware**: Microsoft YARP reverse proxy handler
- **CustomReverseProxyMiddleware**: Custom reverse proxy implementation
- **LiveCodeMiddleware**: Dynamic C# code execution endpoint
- **FrontendMiddleware**: Static file serving for web UI
- **TeapotMiddleware**: HTTP 418 fallback handler
- **HelloWorldMiddleware**: Debug testing endpoint (development only)

**Features (Request Context):**
- **ReverseProxyFeature**: Stores reverse proxy route and configuration
- **PermissionFeature**: Tracks permission status for request

### Tasks (Background Operations)

Located in `Tasks/` directory:

- **IntervalTask_Continuously**: High-frequency background operations
- **IntervalTask_Daily**: Daily maintenance and cleanup
- **IntervalTask_DnsServer**: DNS server operation loop
- **IntervalTask_ReloadDatabase**: Hot-reload database on file changes
- **IntervalTask_RenewCertificates**: Certificate renewal automation (Linux)
- **IntervalTask_UpdateBrandmauer**: Self-update mechanism (Linux)
- **IntervalTask_Startup**: Initialization tasks on application start (Linux)
- **IntervalTask_FortiClient**: FortiClient VPN integration (optional, Linux)

### Helpers and Utilities

- **CertificateUtils**: Certificate generation, parsing, and conversion
- **TotpUtils**: TOTP token generation and validation
- **IpTablesManager**: Linux iptables rule management (Linux only)
- **Utils**: Common utilities (IP address handling, string parsing, etc.)
- **Audit**: Application-wide logging and audit trail
- **ExceptionConverter**: JSON serialization for exceptions

### Endpoints (API Controllers)

Located in `Endpoints/` directory as partial classes of `Endpoint`:

- RESTful CRUD operations for all models
- Authentication endpoints (login, logout, 2FA setup)
- Certificate management endpoints (issue, renew, export)
- Proxy configuration endpoints
- Monitoring endpoints (status, alerts)
- System management endpoints (restart, update)
- File sharing endpoints
- Live code execution endpoints

## Development Guidelines

### Adding a New Model

1. Create class inheriting from `Model` in `Models/`
2. Add collection property to `Database` class
3. Create endpoint partial class in `Endpoints/Endpoint.YourModel.cs`
4. Map CRUD endpoints in `Endpoint.MapAll()`
5. Implement `IOnDeserialize` if post-load initialization needed

### Adding a Background Task

1. Create class inheriting from `IntervalTask` in `Tasks/`
2. Add `[Interval(seconds)]` attribute for periodic execution
3. Implement required methods: `OnStartAsync()`, `OnTickAsync()`, `OnDisposeAsync()`
4. Register in `Program.cs` task registration section

### Adding a Middleware

1. Create middleware class in `Middlewares/`
2. Implement `Invoke(HttpContext context)` with `RequestDelegate next`
3. Add to middleware pipeline in `Program.cs` (order matters!)

### Important Patterns

- **Thread Safety**: All database access must use `Database.Use()` or `Database.UseAsync()`
- **Model Lifecycle**: Models register on creation and must be disposed properly
- **Logging**: Use `Audit` class, never `Console.WriteLine`
- **Unsafe Code**: Project allows unsafe blocks for low-level operations
- **Platform-Specific Code**: Use `#if LINUX`, `#if WINDOWS`, `#if DEBUG` directives

## Use Cases

Brandmauer excels in scenarios requiring consolidated network management:

### Small to Medium Business
- Single application for all network infrastructure needs
- Reduced complexity compared to multiple tools
- Automatic SSL/TLS certificate management
- Built-in monitoring and alerting

### Development Teams
- Reverse proxy for microservices architecture
- Local development with production-like routing
- Dynamic routing for testing environments
- Live code execution for debugging

### Home Lab and Self-Hosting
- Comprehensive network management for homelab
- Free SSL certificates via Let's Encrypt
- DNS and DDNS for internal services
- Firewall and access control

### Edge Computing
- Lightweight single-binary deployment
- Consolidates multiple network functions
- Low resource footprint
- Self-contained with no external dependencies

## Future Roadmap Considerations

While Brandmauer is production-ready, potential enhancements could include:

- Web-based UI for configuration (currently API-driven)
- Metrics and analytics dashboard
- High availability and clustering support
- Additional monitoring plugins
- Docker container support
- Configuration import/export utilities
- Rate limiting and DDoS protection
- Geographic routing and load balancing

## Support and Documentation

- **Build Status**: Automated CI/CD via GitHub Actions
- **Version**: 0.3.1.4 (actively developed)
- **License**: Check repository for licensing information
- **Platform Support**: Linux (primary), Windows (supported)

## Technical Highlights

### What Makes Brandmauer Unique

1. **Unified Platform**: Unlike typical solutions requiring Nginx/Apache + Certbot + Firewall + DNS + Monitoring, Brandmauer provides all functionality in a single binary

2. **JSON-Based Configuration**: Human-readable, version-controllable configuration without complex syntax

3. **Hot-Reload Everything**: Configuration changes apply without service restart

4. **Dual Proxy Engine**: Flexibility to use Microsoft YARP or custom implementation based on needs

5. **Scripting Support**: Inject C# code directly into routing logic for ultimate flexibility

6. **Self-Updating**: Application can update itself on Linux (production environment)

7. **Cross-Platform First**: Designed from the ground up for both Linux and Windows

8. **Developer Experience**: Full debugging support, hot reload, and development-friendly defaults

9. **Production Hardened**: systemd integration, automatic restarts, audit logging, and monitoring

10. **Zero External Dependencies**: Self-contained deployment with all dependencies bundled

## Conclusion

Brandmauer represents a modern approach to network infrastructure management, consolidating essential services into a cohesive, easy-to-deploy platform. Built on .NET 9 with careful attention to security, performance, and developer experience, it serves as both a production-ready solution and a flexible development tool.

Whether deployed in enterprise environments, development labs, or home networks, Brandmauer provides the tools needed to manage, secure, and monitor network traffic with minimal complexity and maximum control.

---

**Project Repository**: D:\1\Development\Brandmauer
**Documentation**: See CLAUDE.md for developer guidance
**Build Status**: [![Build](https://github.com/themelektaus/brandmauer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/themelektaus/brandmauer/actions/workflows/dotnet.yml)
