# Coherent Web Portal - Quick Setup Guide

## Database Configuration

### ✅ Databases Configured

**Primary Database**: `UEMedical_For_R&D`
- Server: 175.107.195.221
- User: Tekno
- Contains: Users, Roles, Permissions, Audit Logs

**Secondary Database**: `CoherentMobApp`
- Server: 175.107.195.221
- User: Tekno
- Contains: Third-Party Clients, Request Logs

---

## 🚀 Quick Setup Steps

### Step 1: Update Security Keys

**Generate JWT Secret (64+ characters):**
```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host "JWT Secret: $secret"
```

**Generate Encryption Keys:**
```powershell
# Generate 256-bit (32 bytes) key
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

**Update `appsettings.json`:**
```json
{
  "Jwt": {
    "Secret": "PASTE_YOUR_GENERATED_JWT_SECRET_HERE"
  },
  "Encryption": {
    "Key": "PASTE_YOUR_GENERATED_ENCRYPTION_KEY_HERE",
    "IV": "PASTE_YOUR_GENERATED_IV_HERE"
  }
}
```

---

### Step 2: Run Database Scripts

**Connect to your SQL Server and run these scripts in order:**

#### A. Primary Database (UEMedical_For_R&D)

```bash
# Using sqlcmd
sqlcmd -S 175.107.195.221 -U Tekno -P "123qwe@" -d "UEMedical_For_R&D" -i "Database/PrimaryDatabase_Schema.sql"
sqlcmd -S 175.107.195.221 -U Tekno -P "123qwe@" -d "UEMedical_For_R&D" -i "Database/PrimaryDatabase_SeedData.sql"
```

**Or using SQL Server Management Studio (SSMS):**
1. Connect to server: 175.107.195.221
2. Login: Tekno / 123qwe@
3. Open `Database/PrimaryDatabase_Schema.sql`
4. Execute (F5)
5. Open `Database/PrimaryDatabase_SeedData.sql`
6. Execute (F5)

#### B. Secondary Database (CoherentMobApp)

```bash
# Using sqlcmd
sqlcmd -S 175.107.195.221 -U Tekno -P "123qwe@" -d "CoherentMobApp" -i "Database/SecondaryDatabase_Schema.sql"
```

**Or using SSMS:**
1. Same connection
2. Open `Database/SecondaryDatabase_Schema.sql`
3. Execute (F5)

---

### Step 3: Build the Project

```bash
# Make sure Visual Studio and IIS Express are CLOSED first!

# Clean previous builds
dotnet clean

# Build solution
dotnet build
```

---

### Step 4: Run the Application

```bash
dotnet run --project "Coherent Web Portal"
```

**Application will start on:**
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5000

---

### Step 5: Test with Swagger

Open browser: **https://localhost:7001/swagger**

#### Test Login:
```
POST /api/auth/login

Body:
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Expected Response:**
```json
{
  "isSuccess": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "user": {
    "username": "admin",
    "roles": ["Admin"],
    "permissions": ["Users.Create", "Users.Read", ...]
  }
}
```

---

## 📋 Database Tables Created

### Primary Database (UEMedical_For_R&D)
- ✅ `Users` - User authentication and profiles
- ✅ `Roles` - System roles (Admin, Doctor, Nurse, etc.)
- ✅ `Permissions` - Fine-grained permissions
- ✅ `UserRoles` - User-role assignments
- ✅ `RolePermissions` - Role-permission mappings
- ✅ `AuditLogs` - ADHICS compliance logging

### Secondary Database (CoherentMobApp)
- ✅ `ThirdPartyClients` - Third-party system registrations (Meditex IVF, etc.)
- ✅ `ThirdPartyRequestLogs` - Third-party API request logs

---

## 🔐 Default Test Users

After running seed data, these users will be available:

| Username | Password | Role | Permissions |
|----------|----------|------|-------------|
| admin | Admin@123 | Admin | All |
| doctor1 | Admin@123 | Doctor | Patient CRUD |
| nurse1 | Admin@123 | Nurse | Patient Read |

⚠️ **IMPORTANT**: Change these passwords immediately in production!

---

## 🔧 Configuration Checklist

Before running in production:

- [ ] Update JWT Secret (64+ characters)
- [ ] Update Encryption Key (32 bytes base64)
- [ ] Update Encryption IV (16 bytes base64)
- [ ] Change default user passwords
- [ ] Review CORS allowed origins
- [ ] Configure HTTPS certificate
- [ ] Set up proper logging destination
- [ ] Configure rate limits
- [ ] Review database connection security
- [ ] Set up backups for both databases

---

## 🌐 Architecture

```
Web Portal (Frontend)
    ↓ HTTPS
Coherent Web Portal API (.NET 8)
    ↓
    ├─→ UEMedical_For_R&D (Primary DB)
    │   ├── Users & Authentication
    │   ├── Roles & Permissions
    │   └── Audit Logs
    │
    └─→ CoherentMobApp (Secondary DB)
        ├── Third-Party Clients
        └── Request Logs

Third-Party Systems (e.g., Meditex IVF)
    ↓ Security Key Auth
Coherent Web Portal API
    ↓
CoherentMobApp (Logs)
```

---

## 📞 Support Files

- **README.md** - Full project documentation
- **API_DOCUMENTATION.md** - Complete API reference
- **SECURITY_CONFIGURATION.md** - Detailed security setup
- **IMPLEMENTATION_GUIDE.md** - Architecture guide
- **PROJECT_SUMMARY.md** - Implementation summary

---

## ⚠️ Important Notes

### Connection String Security
Your connection strings are currently in `appsettings.json`. For production:

1. **Use User Secrets (Development):**
```bash
dotnet user-secrets init --project "Coherent Web Portal"
dotnet user-secrets set "ConnectionStrings:PrimaryDatabase" "Server=175.107.195.221;..." --project "Coherent Web Portal"
```

2. **Use Environment Variables (Production):**
```bash
export ConnectionStrings__PrimaryDatabase="Server=175.107.195.221;..."
export ConnectionStrings__SecondaryDatabase="Server=175.107.195.221;..."
```

3. **Use Azure Key Vault or similar secret management**

### Database Access
- Make sure server 175.107.195.221 is accessible from your machine
- Check firewall rules if you can't connect
- Verify SQL Server allows remote connections
- Test connection with SSMS before running scripts

### Existing Data Warning
⚠️ If `UEMedical_For_R&D` or `CoherentMobApp` already contain tables with the same names, the schema scripts will fail. Options:

1. **Drop existing tables** (will lose data):
```sql
DROP TABLE IF EXISTS Users, Roles, Permissions, UserRoles, RolePermissions, AuditLogs;
```

2. **Use different table names** (modify schema scripts)

3. **Backup existing data** before running scripts

---

## 🚀 Next Steps After Setup

1. **Build Frontend**: Create React/Angular/Blazor frontend
2. **Add Healthcare APIs**: Patient management, appointments, etc.
3. **Configure Meditex Integration**: Register third-party client
4. **Set up Monitoring**: Application Insights, logs
5. **Deploy to Production**: Azure App Service, IIS, Docker

---

## 🎯 Quick Test Commands

```bash
# Test connection to database
sqlcmd -S 175.107.195.221 -U Tekno -P "123qwe@" -Q "SELECT @@VERSION"

# Check if tables exist
sqlcmd -S 175.107.195.221 -U Tekno -P "123qwe@" -d "UEMedical_For_R&D" -Q "SELECT name FROM sys.tables"

# Build and run
dotnet build && dotnet run --project "Coherent Web Portal"

# Test health endpoint
curl https://localhost:7001/health -k
```

---

## 🔥 Common Issues

### Issue: Can't connect to database
**Solution:** Check firewall, VPN connection, SQL Server remote access enabled

### Issue: Build fails with locked files
**Solution:** Close Visual Studio and IIS Express completely

### Issue: Tables already exist
**Solution:** Backup and drop existing tables, or rename new tables in schema

### Issue: JWT validation fails
**Solution:** Make sure JWT secret is at least 32 characters

---

**Ready to start! Run the setup steps in order and you'll have a fully functional ADHICS-compliant API.**
