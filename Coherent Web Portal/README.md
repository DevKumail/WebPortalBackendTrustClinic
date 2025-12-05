# Coherent Web Portal Backend

## Overview
ADHICS-compliant web portal backend built on .NET 8 with comprehensive security features including JWT authentication, role-based access control (RBAC), dual database support, and third-party system integration.

## Architecture

### Layered Architecture
- **Coherent.Domain**: Entity models and domain logic
- **Coherent.Core**: Interfaces, DTOs, and enums
- **Coherent.Infrastructure**: Data access (Dapper), services, middleware
- **Coherent.Application**: Business logic and application services
- **Coherent Web Portal (API)**: Web API controllers and endpoints

### Database Configuration

The system uses two SQL Server databases:

- **Primary Database** (`UEMedical_For_R&D`): User management, authentication, RBAC, audit logs
- **Secondary Database** (`CoherentMobApp`): Third-party integration, mobile app data, request logs

Both databases are hosted on: **175.107.195.221**

### Key Features

#### 1. Multi-Database Support
- Uses Dapper ORM with parameterized queries for SQL injection prevention
- Repository pattern with Unit of Work for transaction management

#### 2. JWT Authentication & RBAC
- JWT tokens with embedded role and permission claims
- Token refresh mechanism with secure refresh tokens
- Configurable token expiry (default: 60 min access, 7 days refresh)
- Custom `[Role]` and `[Permission]` attributes for endpoint protection

#### 3. Third-Party Integration
- Security key validation for external systems
- IP whitelisting
- Rate limiting per client
- Endpoint permission management
- Comprehensive request/response logging

#### 4. ADHICS Compliance Features
- **Encryption**: AES-256 for data at rest, TLS 1.2+ for data in transit
- **Audit Logging**: All authentication, authorization, and data access events
- **Security Headers**: X-Frame-Options, CSP, HSTS, etc.
- **Rate Limiting**: Global and per-client rate limits
- **Password Security**: BCrypt hashing with salt

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server 2019 or later
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd "Coherent Web Portal"
```

2. **Configure Database Connection Strings**

Edit `Coherent Web Portal/appsettings.json`:
```json
"ConnectionStrings": {
  "PrimaryDatabase": "Server=YOUR_SERVER;Database=CoherentWebPortal_Primary;...",
  "SecondaryDatabase": "Server=YOUR_SERVER;Database=CoherentWebPortal_Secondary;..."
}
```

3. **Generate Encryption Keys**

Run the key generation script:
```bash
dotnet run --project KeyGenerator
```

Or manually generate:
```csharp
// AES-256 Key (32 bytes)
var key = new byte[32];
RandomNumberGenerator.Fill(key);
var base64Key = Convert.ToBase64String(key);

// IV (16 bytes)
var iv = new byte[16];
RandomNumberGenerator.Fill(iv);
var base64IV = Convert.ToBase64String(iv);
```

Update `appsettings.json`:
```json
"Encryption": {
  "Key": "YOUR_BASE64_ENCODED_AES_256_KEY",
  "IV": "YOUR_BASE64_ENCODED_INITIALIZATION_VECTOR"
}
```

4. **Configure JWT Secret**

Generate a secure secret (minimum 32 characters):
```json
"Jwt": {
  "Secret": "YOUR_SECURE_JWT_SECRET_KEY_HERE",
  "Issuer": "https://your-domain.com",
  "Audience": "https://your-domain.com"
}
```

5. **Run Database Scripts**

Execute the SQL scripts in order:
```bash
# Primary Database
sqlcmd -S YOUR_SERVER -d CoherentWebPortal_Primary -i Database/PrimaryDatabase_Schema.sql
sqlcmd -S YOUR_SERVER -d CoherentWebPortal_Primary -i Database/PrimaryDatabase_SeedData.sql

# Secondary Database
sqlcmd -S YOUR_SERVER -d CoherentWebPortal_Secondary -i Database/SecondaryDatabase_Schema.sql
```

6. **Build and Run**
```bash
dotnet build
dotnet run --project "Coherent Web Portal"
```

The API will be available at:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001
- Swagger: https://localhost:7001/swagger

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - User logout
- `GET /api/auth/me` - Get current user info

### Audit (Admin/Auditor only)
- `GET /api/audit/logs` - Get audit logs by date range

### Third-Party Integration
- `POST /api/third-party/data` - Third-party data endpoint
- `GET /api/third-party/health` - Health check for third-party

### Health
- `GET /api/health` - API health check
- `GET /health` - Detailed health check

## Security Configuration

### CORS
Configure allowed origins in `appsettings.json`:
```json
"Cors": {
  "AllowedOrigins": [
    "https://your-frontend.com"
  ]
}
```

### Rate Limiting
Default: 100 requests per minute per IP
Configure in `Program.cs`:
```csharp
PermitLimit = 100,
Window = TimeSpan.FromMinutes(1)
```

### Third-Party Authentication

Headers required:
```
X-Client-ID: <client-id>
X-Security-Key: <security-key>
```

## ADHICS Compliance

### Data Encryption
- **At Rest**: AES-256 encryption for sensitive fields
- **In Transit**: TLS 1.2+ enforced (RequireHttpsMetadata = true)

### Audit Logging
All events logged with:
- User ID and username
- Action performed
- Entity type and ID
- Old/new values (for updates)
- IP address and user agent
- Timestamp
- Success/failure status
- Risk level (Low, Medium, High, Critical)

### Access Control
- Role-based access control (RBAC)
- Permission-based fine-grained control
- Multi-layer enforcement (API + data access)

### Session Management
- Secure JWT tokens
- Automatic token expiry
- Refresh token rotation
- Logout invalidates refresh tokens

## Development

### Adding New Entities
1. Create entity in `Coherent.Domain/Entities`
2. Add repository in `Coherent.Infrastructure/Repositories`
3. Create service interface in `Coherent.Core/Interfaces`
4. Implement service in `Coherent.Infrastructure/Services`
5. Register in `Program.cs`

### Adding New Endpoints
1. Create controller in `Controllers` folder
2. Add `[Authorize]` attribute
3. Add `[Role]` or `[Permission]` attributes as needed
4. Inject required services
5. Implement audit logging for sensitive operations

## Deployment

### Production Checklist
- [ ] Update all configuration secrets
- [ ] Enable HTTPS-only in production
- [ ] Configure production database connections
- [ ] Set appropriate CORS origins
- [ ] Configure production logging (Azure App Insights, etc.)
- [ ] Set up database backups
- [ ] Enable application monitoring
- [ ] Review and adjust rate limits
- [ ] Set up disaster recovery procedures

### Environment Variables (Alternative to appsettings)
```bash
JWT__SECRET=your-secret
JWT__ISSUER=https://your-domain.com
CONNECTIONSTRINGS__PRIMARYDATABASE=connection-string
CONNECTIONSTRINGS__SECONDARYDATABASE=connection-string
```

## Troubleshooting

### JWT Token Issues
- Verify secret key matches between token generation and validation
- Check token expiry time
- Ensure clock synchronization (ClockSkew = TimeSpan.Zero)

### Database Connection Issues
- Verify SQL Server is running
- Check connection strings
- Ensure SQL Server allows remote connections
- Verify firewall settings

### Third-Party Authentication Fails
- Verify security key is correctly hashed
- Check IP whitelist
- Verify client is active
- Check rate limits

## License
Proprietary - All rights reserved

## Support
For support, contact: support@coherent.local
