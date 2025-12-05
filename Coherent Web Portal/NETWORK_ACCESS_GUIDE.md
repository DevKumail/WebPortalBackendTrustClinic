# Network Access Setup Guide

## Problem
Aap IIS Express profile use kar rahe hain jo network access support nahi karta.

## Solution: Profile Change Karen

### Visual Studio 2022 Me:

1. **Top Toolbar Dekho** (code editor ke upar)
2. **Green Play button ke BAGAL me** ek dropdown hai jisme likha hai "IIS Express"
3. **Us dropdown par click karen**
4. **"http"** ya **"Coherent Web Portal (http)"** select karen
5. **Ab F5 press karen ya Green Play button click karen**

### Screenshot Reference:
```
[▶ IIS Express ▼]  ← Is dropdown par click karen
     │
     ├─ IIS Express              ❌ YE SELECTED HAI (galat)
     ├─ http                     ✅ YE SELECT KARO
     ├─ https                    ✅ Ya ye (HTTPS ke liye)
     └─ Coherent Web Portal      
```

## Success Check

Jab correct profile select hogi, console me ye dikhega:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:5162
```

**Port 15678 NAHI dikhna chahiye!**

## Network Access

### 1. Apna IP Address Pata Karen:
```powershell
ipconfig
```
Example: 192.168.1.100

### 2. Firewall Rule (Administrator PowerShell):
```powershell
netsh advfirewall firewall add rule name="ASP.NET Port 5162" dir=in action=allow protocol=TCP localport=5162
```

### 3. Dusra Developer Access Karega:
```
http://YOUR_IP:5162/swagger
```
Example: http://192.168.1.100:5162/swagger

## Troubleshooting

### Agar Profile Dropdown Nahi Dikh Raha:
- **View > Toolbars > Standard** enable karen
- Ya **Debug > Coherent Web Portal Debug Properties** me jao
- Launch profile manually select karen

### Agar Console Nahi Dikh Raha:
Run karne ke baad **View > Output** ya **Ctrl+Alt+O** press karen
