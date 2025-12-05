# =============================================
# Security Keys Generator for Coherent Web Portal
# Run this script to generate all required security keys
# =============================================

Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host "  Coherent Web Portal Security Keys"  -ForegroundColor Cyan
Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host ""

# Generate JWT Secret (64 bytes = 512 bits)
Write-Host "Generating JWT Secret (64 bytes)..." -ForegroundColor Yellow
$jwtBytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtBytes)
$jwtSecret = [Convert]::ToBase64String($jwtBytes)

Write-Host "JWT Secret (copy this to appsettings.json):" -ForegroundColor Green
Write-Host $jwtSecret -ForegroundColor White
Write-Host ""

# Generate AES-256 Encryption Key (32 bytes = 256 bits)
Write-Host "Generating AES-256 Encryption Key (32 bytes)..." -ForegroundColor Yellow
$keyBytes = New-Object byte[] 32
$rng.GetBytes($keyBytes)
$encryptionKey = [Convert]::ToBase64String($keyBytes)

Write-Host "Encryption Key (copy this to appsettings.json):" -ForegroundColor Green
Write-Host $encryptionKey -ForegroundColor White
Write-Host ""

# Generate Initialization Vector (16 bytes = 128 bits)
Write-Host "Generating Initialization Vector (16 bytes)..." -ForegroundColor Yellow
$ivBytes = New-Object byte[] 16
$rng.GetBytes($ivBytes)
$encryptionIV = [Convert]::ToBase64String($ivBytes)

Write-Host "Encryption IV (copy this to appsettings.json):" -ForegroundColor Green
Write-Host $encryptionIV -ForegroundColor White
Write-Host ""

Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host "  Update appsettings.json"  -ForegroundColor Cyan
Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host ""

$jsonConfig = @"
{
  "Jwt": {
    "Secret": "$jwtSecret",
    "Issuer": "https://coherent-web-portal.local",
    "Audience": "https://coherent-web-portal.local",
    "AccessTokenExpiryMinutes": "60",
    "RefreshTokenExpiryDays": "7"
  },
  "Encryption": {
    "Key": "$encryptionKey",
    "IV": "$encryptionIV"
  }
}
"@

Write-Host "Copy this JSON configuration:" -ForegroundColor Yellow
Write-Host $jsonConfig -ForegroundColor White
Write-Host ""

# Save to file
$outputFile = "generated-keys.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$fileContent = @"
========================================
Coherent Web Portal - Security Keys
Generated: $timestamp
========================================

IMPORTANT: Keep these keys SECURE!
- Never commit to source control
- Store in secure key management (Azure Key Vault, etc.)
- Use different keys for Dev/Staging/Production

========================================
JWT SECRET (64 bytes, base64 encoded):
========================================
$jwtSecret

========================================
ENCRYPTION KEY (32 bytes, base64 encoded):
========================================
$encryptionKey

========================================
ENCRYPTION IV (16 bytes, base64 encoded):
========================================
$encryptionIV

========================================
APPSETTINGS.JSON CONFIGURATION:
========================================
$jsonConfig

========================================
HOW TO USE:
========================================
1. Open Coherent Web Portal\appsettings.json
2. Replace the values in Jwt and Encryption sections
3. Save the file
4. Build and run the application

========================================
SECURITY NOTES:
========================================
- JWT Secret: Used to sign and verify JWT tokens
- Encryption Key: Used for AES-256 encryption of sensitive data
- Encryption IV: Initialization vector for AES encryption

- All keys are cryptographically random
- Keys are base64 encoded for easy storage
- Change keys annually or after security incident

"@

$fileContent | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "=========================================="  -ForegroundColor Cyan
Write-Host ""
Write-Host "Keys saved to: $outputFile" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Copy the JSON configuration above" -ForegroundColor White
Write-Host "2. Update appsettings.json with the new values" -ForegroundColor White
Write-Host "3. Build the project: dotnet build" -ForegroundColor White
Write-Host "4. Run database scripts (see SETUP_GUIDE.md)" -ForegroundColor White
Write-Host "5. Start the application: dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "=========================================="  -ForegroundColor Cyan
