# Security Configuration Guide - ADHICS Compliance

## Overview
This guide provides step-by-step instructions for configuring all security features in compliance with Abu Dhabi Healthcare Information and Cyber Security Standard (ADHICS).

---

## 1. Encryption Configuration

### Generate AES-256 Keys

**Using PowerShell:**
```powershell
# Generate 256-bit (32 bytes) encryption key
$key = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($key)
$base64Key = [Convert]::ToBase64String($key)
Write-Host "Encryption Key: $base64Key"

# Generate 128-bit (16 bytes) IV
$iv = New-Object byte[] 16
$rng.GetBytes($iv)
$base64IV = [Convert]::ToBase64String($iv)
Write-Host "IV: $base64IV"
```

**Using C# Console App:**
```csharp
using System.Security.Cryptography;

var key = new byte[32]; // 256 bits
var iv = new byte[16];  // 128 bits

RandomNumberGenerator.Fill(key);
RandomNumberGenerator.Fill(iv);

Console.WriteLine($"Key: {Convert.ToBase64String(key)}");
Console.WriteLine($"IV: {Convert.ToBase64String(iv)}");
```

**Update appsettings.json:**
```json
{
  "Encryption": {
    "Key": "YOUR_GENERATED_BASE64_KEY_HERE",
    "IV": "YOUR_GENERATED_BASE64_IV_HERE"
  }
}
```

⚠️ **IMPORTANT**: 
- Store these values securely (Azure Key Vault, AWS Secrets Manager, etc.)
- Never commit encryption keys to source control
- Use different keys for development, staging, and production
- Rotate keys annually or after any security incident

---

## 2. JWT Configuration

### Generate Secure JWT Secret

**Minimum Requirements:**
- At least 64 characters
- Cryptographically random
- Unique per environment

**PowerShell:**
```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host "JWT Secret: $secret"
```

**C#:**
```csharp
var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
Console.WriteLine($"JWT Secret: {secret}");
```

### JWT Configuration

**appsettings.json:**
```json
{
  "Jwt": {
    "Secret": "YOUR_GENERATED_SECRET_HERE",
    "Issuer": "https://coherent-web-portal.com",
    "Audience": "https://coherent-web-portal.com",
    "AccessTokenExpiryMinutes": "60",
    "RefreshTokenExpiryDays": "7"
  }
}
```

**ADHICS Requirements Met:**
- ✅ Strong cryptographic algorithm (HS256)
- ✅ Token expiration (1 hour for access, 7 days for refresh)
- ✅ Secure token storage (httpOnly cookies recommended for web)
- ✅ Token revocation support (refresh token invalidation)

---

## 3. Database Security

### Connection String Security

**Development (Windows Authentication):**
```json
{
  "ConnectionStrings": {
    "PrimaryDatabase": "Server=localhost;Database=CoherentWebPortal_Primary;Integrated Security=true;TrustServerCertificate=True;Encrypt=True;",
    "SecondaryDatabase": "Server=localhost;Database=CoherentWebPortal_Secondary;Integrated Security=true;TrustServerCertificate=True;Encrypt=True;"
  }
}
```

**Production (SQL Authentication with Encryption):**
```json
{
  "ConnectionStrings": {
    "PrimaryDatabase": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=CoherentWebPortal_Primary;Persist Security Info=False;User ID=sqladmin;Password=YOUR_STRONG_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "SecondaryDatabase": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=CoherentWebPortal_Secondary;..."
  }
}
```

### SQL Server Security Configuration

**1. Enable Encryption:**
```sql
-- Force encryption for all connections
USE master;
GO
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO
EXEC sp_configure 'force encryption', 1;
RECONFIGURE;
GO
```

**2. Create Service Account:**
```sql
-- Create dedicated SQL login for application
CREATE LOGIN CoherentWebPortalUser 
WITH PASSWORD = 'StrongPassword123!@#';
GO

USE CoherentWebPortal_Primary;
CREATE USER CoherentWebPortalUser FOR LOGIN CoherentWebPortalUser;
GO

-- Grant minimum required permissions
ALTER ROLE db_datareader ADD MEMBER CoherentWebPortalUser;
ALTER ROLE db_datawriter ADD MEMBER CoherentWebPortalUser;
GO
```

**3. Enable Auditing:**
```sql
-- Enable SQL Server audit
CREATE SERVER AUDIT CoherentWebPortalAudit
TO FILE (FILEPATH = 'C:\SQLAudit\', MAXSIZE = 1024 MB, MAX_ROLLOVER_FILES = 10)
WITH (ON_FAILURE = CONTINUE);
GO

ALTER SERVER AUDIT CoherentWebPortalAudit
WITH (STATE = ON);
GO
```

---

## 4. Password Policy

### ADHICS Password Requirements

**Configured in appsettings.json:**
```json
{
  "ADHICS": {
    "PasswordMinLength": 12,
    "MaxLoginAttempts": 3,
    "PasswordExpiryDays": 90,
    "PasswordHistoryCount": 5
  }
}
```

**Password Complexity Rules:**
- Minimum 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character
- Cannot contain username
- Cannot be one of last 5 passwords

**BCrypt Configuration:**
```csharp
// Work factor of 12 provides strong security
BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
```

---

## 5. HTTPS/TLS Configuration

### Development Certificate

```bash
# Trust development certificate
dotnet dev-certs https --trust
```

### Production Certificate

**Option 1: Let's Encrypt (Free)**
```bash
# Install certbot
sudo apt-get install certbot

# Generate certificate
sudo certbot certonly --standalone -d coherent-web-portal.com
```

**Option 2: Commercial Certificate**
- Purchase from trusted CA (DigiCert, GlobalSign, etc.)
- Follow CA's validation process
- Install certificate on server

**Configure in appsettings.Production.json:**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/path/to/certificate.pfx",
          "Password": "certificate-password"
        }
      }
    }
  }
}
```

### TLS Version Enforcement

**Program.cs:**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
```

---

## 6. CORS Configuration

### Production CORS Settings

**Restrict to specific origins:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.coherent-web-portal.com",
      "https://admin.coherent-web-portal.com"
    ]
  }
}
```

⚠️ **Never use** `AllowAnyOrigin()` in production!

---

## 7. Rate Limiting

### Global Rate Limit

**Current Configuration:**
- 100 requests per minute per IP
- Sliding window algorithm

**Adjust in Program.cs:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // Adjust based on load
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

### Third-Party Rate Limits

**Configured per client in database:**
```sql
UPDATE ThirdPartyClients 
SET MaxRequestsPerMinute = 200
WHERE ClientId = 'high-volume-client';
```

---

## 8. Third-Party Security

### Registering Third-Party Clients

**1. Generate Security Key:**
```csharp
var securityKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
Console.WriteLine($"Security Key: {securityKey}");

// Share this with the client (securely via encrypted email or secure portal)
```

**2. Hash Security Key:**
```csharp
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(securityKey));
var securityKeyHash = Convert.ToBase64String(hashBytes);

// Store this in database
```

**3. Insert Client:**
```sql
INSERT INTO ThirdPartyClients 
(Id, ClientName, ClientId, ApiKeyHash, IpWhitelist, IsActive, 
 SecurityKeyHash, SecurityKeyExpiry, MaxRequestsPerMinute, 
 AllowedEndpoints, DataAccessLevel)
VALUES 
(NEWID(), 
 'Hospital System A',
 'hospital-system-a',
 'api-key-hash',
 '192.168.1.100,192.168.1.101,10.0.0.0/24', -- IP whitelist
 1,
 'YOUR_HASHED_SECURITY_KEY',
 DATEADD(YEAR, 1, GETUTCDATE()),
 100,
 '["api/third-party/data", "api/third-party/patients"]', -- Allowed endpoints
 'Read'); -- Data access level
```

### IP Whitelisting

**Formats Supported:**
- Single IP: `192.168.1.100`
- Multiple IPs: `192.168.1.100,192.168.1.101`
- CIDR notation: `10.0.0.0/24`
- Allow all: `*` (NOT RECOMMENDED for production)

---

## 9. Audit Logging

### Automatic Logging

All operations are automatically logged:
- Authentication events
- Authorization failures
- Data access
- Configuration changes
- Third-party interactions

### Manual Logging for Custom Operations

```csharp
await _auditService.LogActionAsync(
    userId: currentUserId,
    username: currentUsername,
    action: "CUSTOM_OPERATION",
    entityType: "EntityName",
    entityId: entityId,
    oldValues: JsonSerializer.Serialize(oldData),
    newValues: JsonSerializer.Serialize(newData),
    ipAddress: httpContext.Connection.RemoteIpAddress?.ToString() ?? "",
    userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
    databaseSource: "Primary",
    complianceCategory: "DataModification",
    riskLevel: "Medium",
    isSuccess: true
);
```

### Audit Log Retention

**ADHICS Requirement: 365 days minimum**

**Configured in appsettings.json:**
```json
{
  "ADHICS": {
    "AuditRetentionDays": 365
  }
}
```

**Cleanup Script (run monthly):**
```sql
DELETE FROM AuditLogs
WHERE Timestamp < DATEADD(DAY, -365, GETUTCDATE());
```

---

## 10. Environment-Specific Configuration

### Development
- Use development certificates
- Detailed error messages
- Swagger UI enabled
- Relaxed CORS

### Staging
- Production-like certificates
- Limited error details
- Swagger UI enabled (authenticated)
- Strict CORS

### Production
- Valid SSL certificates
- Minimal error exposure
- Swagger UI disabled or authenticated
- Strict CORS
- Enhanced monitoring

**Use environment variables or Azure Key Vault:**
```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Override settings
export Jwt__Secret="production-secret"
export ConnectionStrings__PrimaryDatabase="production-connection"
```

---

## 11. Security Headers

**Already configured in Program.cs:**

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

### Additional Headers (Optional)

```csharp
context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
```

---

## 12. Security Checklist

### Pre-Production Security Audit

- [ ] All default passwords changed
- [ ] Encryption keys generated and secured
- [ ] JWT secret is strong and unique
- [ ] Database connections use encryption
- [ ] SQL Server audit enabled
- [ ] HTTPS enforced (TLS 1.2+)
- [ ] CORS configured for production domains
- [ ] Rate limiting tested
- [ ] Third-party clients registered with strong keys
- [ ] IP whitelisting configured
- [ ] Audit logging verified
- [ ] Error handling doesn't expose sensitive info
- [ ] Security headers configured
- [ ] Swagger UI secured or disabled
- [ ] File upload restrictions (if applicable)
- [ ] SQL injection prevention tested
- [ ] XSS prevention tested
- [ ] CSRF protection implemented (for cookie auth)
- [ ] Penetration testing completed
- [ ] Security incident response plan documented

---

## 13. Incident Response

### Security Breach Procedure

**1. Immediate Actions:**
- Disable compromised accounts
- Revoke affected tokens
- Block suspicious IP addresses
- Enable enhanced logging

**2. Investigation:**
- Review audit logs
- Analyze attack vectors
- Identify compromised data

**3. Remediation:**
- Patch vulnerabilities
- Reset affected credentials
- Rotate encryption keys
- Update security policies

**4. Notification:**
- Inform affected users
- Report to ADHICS authorities (if required)
- Document incident

### Emergency Contacts

- Security Team: security@coherent.local
- ADHICS Compliance: compliance@coherent.local
- IT Operations: ops@coherent.local

---

## 14. Regular Security Maintenance

### Daily
- Review failed authentication attempts
- Check for unusual API activity
- Monitor rate limit violations

### Weekly
- Analyze audit logs for anomalies
- Review third-party access patterns
- Check system health and performance

### Monthly
- Review user access rights
- Audit role assignments
- Test backup and recovery
- Update dependencies

### Quarterly
- Security patch review and application
- Access control audit
- Penetration testing
- ADHICS compliance review

### Annually
- Comprehensive security audit
- Encryption key rotation
- Policy review and updates
- Third-party security assessment

---

## Support

For security configuration assistance:
- Email: security@coherent.local
- Documentation: https://docs.coherent-web-portal.com/security
