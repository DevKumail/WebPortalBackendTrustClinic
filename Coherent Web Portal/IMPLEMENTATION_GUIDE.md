# Coherent Web Portal - Backend Implementation Guide

## Complete ADHICS-Compliant Backend Architecture

This comprehensive guide provides detailed implementation instructions for the Coherent Web Portal backend system.

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Security Implementation](#security-implementation)
3. [Database Configuration](#database-configuration)
4. [Authentication & Authorization](#authentication--authorization)
5. [Third-Party Integration](#third-party-integration)
6. [ADHICS Compliance](#adhics-compliance)
7. [Deployment Guide](#deployment-guide)

---

## Architecture Overview

### Project Structure
```
Coherent Web Portal/
├── Coherent.Domain/              # Domain entities and models
│   └── Entities/
│       ├── User.cs
│       ├── Role.cs
│       ├── Permission.cs
│       ├── AuditLog.cs
│       ├── ThirdPartyClient.cs
│       └── ThirdPartyRequestLog.cs
│
├── Coherent.Core/                # Core interfaces and DTOs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IAuthService.cs
│   │   ├── IJwtTokenService.cs
│   │   ├── IEncryptionService.cs
│   │   ├── IAuditService.cs
│   │   └── IThirdPartyService.cs
│   ├── DTOs/
│   └── Enums/
│
├── Coherent.Infrastructure/      # Data access and services
│   ├── Data/
│   │   ├── DatabaseConnectionFactory.cs
│   │   └── UnitOfWork.cs
│   ├── Repositories/
│   │   ├── BaseRepository.cs
│   │   ├── UserRepository.cs
│   │   ├── AuditLogRepository.cs
│   │   └── ThirdPartyClientRepository.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── JwtTokenService.cs
│   │   ├── EncryptionService.cs
│   │   ├── AuditService.cs
│   │   └── ThirdPartyService.cs
│   ├── Middleware/
│   │   ├── JwtMiddleware.cs
│   │   └── ThirdPartyAuthMiddleware.cs
│   └── Authorization/
│       ├── RoleAttribute.cs
│       └── PermissionAttribute.cs
│
├── Coherent.Application/         # Business logic layer
│
└── Coherent Web Portal/          # Web API
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── AuditController.cs
    │   ├── ThirdPartyController.cs
    │   └── HealthController.cs
    └── Program.cs
```

### Technology Stack
- **.NET 8.0**: Latest LTS version
- **Dapper ORM**: High-performance micro-ORM
- **SQL Server**: Dual database architecture
- **JWT**: Token-based authentication
- **BCrypt**: Password hashing
- **AES-256**: Data encryption
- **Serilog**: Structured logging

---

## Security Implementation

### 1. Password Security (ADHICS Compliant)

**BCrypt Hashing Implementation:**
```csharp
// In EncryptionService.cs
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
}

public bool VerifyPassword(string password, string passwordHash)
{
    return BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

**Requirements:**
- Minimum 12 characters
- Must include uppercase, lowercase, numbers, and special characters
- BCrypt work factor: 12 (secure against brute force)
- Passwords never stored in plain text

### 2. Data Encryption (AES-256)

**Generate Encryption Keys:**
```powershell
# PowerShell script to generate keys
$key = New-Object byte[] 32
$iv = New-Object byte[] 16
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($iv)
[Convert]::ToBase64String($key)  # Use this for Encryption:Key
[Convert]::ToBase64String($iv)   # Use this for Encryption:IV
```

**Encrypt Sensitive Data:**
```csharp
// Example: Encrypting patient SSN
var encryptedSSN = _encryptionService.Encrypt(patient.SSN);
patient.SSN = encryptedSSN;
```

### 3. HTTPS Enforcement (TLS 1.2+)

**Configuration in Program.cs:**
```csharp
options.RequireHttpsMetadata = true; // Force HTTPS
app.UseHttpsRedirection();
```

**Production Certificate Setup:**
```bash
# For development
dotnet dev-certs https --trust

# For production - use proper SSL certificate
# Configure in appsettings.Production.json
```

### 4. Security Headers

All responses include ADHICS-required security headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=31536000`
- `Content-Security-Policy: default-src 'self'`

---

## Database Configuration

### Step 1: Create Databases

```sql
-- Create Primary Database
CREATE DATABASE CoherentWebPortal_Primary;
GO

-- Create Secondary Database
CREATE DATABASE CoherentWebPortal_Secondary;
GO
```

### Step 2: Run Schema Scripts

**Primary Database:**
```bash
sqlcmd -S localhost -d CoherentWebPortal_Primary -i Database/PrimaryDatabase_Schema.sql
sqlcmd -S localhost -d CoherentWebPortal_Primary -i Database/PrimaryDatabase_SeedData.sql
```

**Secondary Database:**
```bash
sqlcmd -S localhost -d CoherentWebPortal_Secondary -i Database/SecondaryDatabase_Schema.sql
```

### Step 3: Configure Connection Strings

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "PrimaryDatabase": "Server=localhost;Database=CoherentWebPortal_Primary;Integrated Security=true;TrustServerCertificate=True;",
    "SecondaryDatabase": "Server=localhost;Database=CoherentWebPortal_Secondary;Integrated Security=true;TrustServerCertificate=True;"
  }
}
```

**For Azure SQL Database:**
```json
{
  "ConnectionStrings": {
    "PrimaryDatabase": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=CoherentWebPortal_Primary;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "SecondaryDatabase": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=CoherentWebPortal_Secondary;..."
  }
}
```

---

## Authentication & Authorization

### JWT Token Configuration

**Generate JWT Secret:**
```csharp
// Minimum 32 characters, use cryptographically secure random string
var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
```

**appsettings.json:**
```json
{
  "Jwt": {
    "Secret": "your-generated-secret-here-minimum-32-characters",
    "Issuer": "https://coherent-web-portal.com",
    "Audience": "https://coherent-web-portal.com",
    "AccessTokenExpiryMinutes": "60",
    "RefreshTokenExpiryDays": "7"
  }
}
```

### Token Structure

**Access Token Claims:**
```json
{
  "nameid": "user-guid",
  "unique_name": "username",
  "email": "user@email.com",
  "role": ["Admin", "Doctor"],
  "Permission": ["Users.Read", "Patients.Create"],
  "FirstName": "John",
  "LastName": "Doe",
  "exp": 1234567890,
  "iss": "https://coherent-web-portal.com",
  "aud": "https://coherent-web-portal.com"
}
```

### Using Authentication

**Login Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Login successful",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "accessTokenExpiry": "2024-12-03T15:00:00Z",
  "refreshTokenExpiry": "2024-12-10T14:00:00Z",
  "user": {
    "id": "guid",
    "username": "admin",
    "email": "admin@coherent.local",
    "roles": ["Admin"],
    "permissions": ["Users.Create", "Users.Read", ...]
  }
}
```

**Using Token in Requests:**
```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Role-Based Access Control

**Controller Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication
public class UsersController : ControllerBase
{
    [HttpGet]
    [Role("Admin", "Doctor")] // Only Admin or Doctor can access
    public async Task<IActionResult> GetUsers() { }
    
    [HttpPost]
    [Permission("Users.Create")] // Requires specific permission
    public async Task<IActionResult> CreateUser() { }
    
    [HttpDelete("{id}")]
    [Role("Admin")] // Only Admin
    [Permission("Users.Delete")] // AND has delete permission
    public async Task<IActionResult> DeleteUser(Guid id) { }
}
```

---

## Third-Party Integration

### Registering a Third-Party Client

**Insert into Secondary Database:**
```sql
-- Generate security key first
DECLARE @SecurityKey NVARCHAR(100) = 'your-generated-security-key';
DECLARE @SecurityKeyHash NVARCHAR(500);

-- Hash will be computed by application
-- For testing, use BCrypt or SHA256

INSERT INTO ThirdPartyClients 
(Id, ClientName, ClientId, ApiKeyHash, IpWhitelist, IsActive, SecurityKeyHash, 
 SecurityKeyExpiry, MaxRequestsPerMinute, AllowedEndpoints, DataAccessLevel)
VALUES 
(NEWID(), 
 'Hospital System A', 
 'hospital-system-a', 
 'api-key-hash-here',
 '192.168.1.100,192.168.1.101', -- Allowed IPs
 1,
 'security-key-hash-here',
 DATEADD(YEAR, 1, GETUTCDATE()),
 100, -- 100 requests per minute
 '["api/third-party/data", "api/third-party/health"]',
 'Read');
```

### Third-Party Authentication

**Request Format:**
```http
POST /api/third-party/data
X-Client-ID: hospital-system-a
X-Security-Key: your-security-key
Content-Type: application/json

{
  "query": "patient-data",
  "parameters": {}
}
```

**Middleware Validation Flow:**
1. Extract `X-Client-ID` and `X-Security-Key` headers
2. Validate client exists and is active
3. Verify security key hash matches
4. Check IP whitelist
5. Verify security key not expired
6. Check rate limit
7. Verify endpoint is allowed
8. Log request details
9. Process request
10. Log response details

---

## ADHICS Compliance

### Audit Logging

**All operations are automatically logged:**

```csharp
// Example: Login audit
await _auditService.LogActionAsync(
    userId: user.Id,
    username: user.Username,
    action: "LOGIN_SUCCESS",
    entityType: "User",
    entityId: user.Id.ToString(),
    ipAddress: "192.168.1.100",
    userAgent: "Mozilla/5.0...",
    databaseSource: "Primary",
    complianceCategory: "Authentication",
    riskLevel: "Low",
    isSuccess: true
);

// Example: Failed access attempt
await _auditService.LogActionAsync(
    userId: null,
    username: "unknown",
    action: "UNAUTHORIZED_ACCESS_ATTEMPT",
    entityType: "Patients",
    entityId: patientId,
    ipAddress: ipAddress,
    userAgent: userAgent,
    databaseSource: "Primary",
    complianceCategory: "Authorization",
    riskLevel: "High",
    isSuccess: false,
    errorMessage: "User does not have required permission"
);
```

### Query Audit Logs

```csharp
[HttpGet("logs")]
[Role("Admin", "Auditor")]
public async Task<IActionResult> GetAuditLogs(
    DateTime from, DateTime to, string? username = null)
{
    var logs = await _auditService.GetAuditLogsAsync(from, to, username);
    return Ok(logs);
}
```

### Compliance Categories

- **Authentication**: Login, logout, token refresh
- **Authorization**: Access attempts, permission checks
- **DataAccess**: CRUD operations on sensitive data
- **Configuration**: System configuration changes
- **ThirdParty**: External system interactions

### Risk Levels

- **Low**: Normal operations (login, read operations)
- **Medium**: Data modifications, failed auth attempts
- **High**: Unauthorized access attempts, bulk operations
- **Critical**: Security breaches, system failures

---

## Deployment Guide

### Development Environment

```bash
# 1. Restore packages
dotnet restore

# 2. Build solution
dotnet build

# 3. Run migrations/scripts
# Execute SQL scripts as shown above

# 4. Run application
dotnet run --project "Coherent Web Portal"
```

### Production Deployment

**1. Configuration Management**

Use environment variables or Azure Key Vault:

```bash
# Set environment variables
export JWT__SECRET="production-secret"
export ConnectionStrings__PrimaryDatabase="production-connection"
export Encryption__Key="production-encryption-key"
```

**2. Docker Deployment**

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Coherent Web Portal.dll"]
```

**3. Azure App Service**

```bash
# Publish to Azure
dotnet publish -c Release
az webapp up --name coherent-web-portal --resource-group YourResourceGroup
```

**4. Production Checklist**

- [ ] Update all default passwords
- [ ] Generate production JWT secret (64+ characters)
- [ ] Generate production encryption keys
- [ ] Configure production database connections
- [ ] Set up SSL/TLS certificates
- [ ] Configure CORS for production domains
- [ ] Enable Application Insights or logging
- [ ] Set up database backups (daily recommended)
- [ ] Configure rate limiting for production load
- [ ] Review and update IP whitelists
- [ ] Set up health check monitoring
- [ ] Configure auto-scaling rules
- [ ] Document disaster recovery procedures

### Performance Optimization

**1. Database Indexing**
All critical queries are indexed (see schema files)

**2. Connection Pooling**
Enabled by default in SQL Server connection strings

**3. Caching Strategy**
```csharp
// Add Redis caching for frequently accessed data
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"];
});
```

**4. Rate Limiting**
Configured at 100 requests/minute per IP (adjustable)

---

## Testing

### Unit Testing Example

```csharp
public class AuthServiceTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var authService = CreateAuthService();
        var request = new LoginRequest 
        { 
            Username = "admin", 
            Password = "Admin@123" 
        };
        
        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1", "Test");
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AccessToken);
    }
}
```

### API Testing with Postman

Import the provided Postman collection for complete API testing.

---

## Support and Maintenance

### Monitoring

**1. Application Logs**
Location: `logs/coherent-web-portal-YYYYMMDD.txt`

**2. Audit Logs**
Database: `PrimaryDatabase.AuditLogs` table

**3. Third-Party Logs**
Database: `SecondaryDatabase.ThirdPartyRequestLogs` table

### Regular Maintenance Tasks

- **Daily**: Review failed authentication attempts
- **Weekly**: Analyze audit logs for anomalies
- **Monthly**: Review third-party access patterns
- **Quarterly**: Security audit and penetration testing
- **Yearly**: ADHICS compliance review

---

## Troubleshooting

### Common Issues

**1. JWT Token Validation Fails**
- Verify secret key matches in configuration
- Check token expiry
- Ensure system clocks are synchronized

**2. Database Connection Issues**
- Verify SQL Server is running
- Check firewall rules
- Validate connection strings
- Test with SQL Server Management Studio

**3. Third-Party Auth Fails**
- Verify client ID and security key
- Check IP whitelist
- Verify security key not expired
- Review rate limits

### Log Analysis

```bash
# Search for failed logins
grep "LOGIN_FAILED" logs/coherent-web-portal-*.txt

# Check high-risk activities
grep "Critical\|High" logs/coherent-web-portal-*.txt
```

---

## Contact

For technical support: tech-support@coherent.local
For security concerns: security@coherent.local
For ADHICS compliance: compliance@coherent.local
