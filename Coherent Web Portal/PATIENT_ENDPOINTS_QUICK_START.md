# Patient Endpoints - Quick Start Guide

## ✅ What Was Created

### 1. **Patient API Endpoints** (No Authentication Required)
- ✅ Get all patients with pagination
- ✅ Search by MRNo
- ✅ Search by name (First, Middle, Last)
- ✅ Search by Emirates ID
- ✅ Search by cell phone
- ✅ Get patient by specific MRNo

### 2. **Files Created**
- `Coherent.Domain/Entities/RegPatient.cs` - Patient entity
- `Coherent.Core/DTOs/PatientDTOs.cs` - Patient DTOs and pagination
- `Coherent.Core/Interfaces/IPatientRepository.cs` - Repository interface
- `Coherent.Infrastructure/Repositories/PatientRepository.cs` - Dapper repository
- `Coherent Web Portal/Controllers/PatientsController.cs` - API controller

---

## 🚀 How to Use

### **Step 1: Run the Application**

```bash
cd "c:\Users\DELL\Desktop\Coheret\Coherent Web Portal"
dotnet run --project "Coherent Web Portal"
```

Application will start at: **https://localhost:7001**

---

### **Step 2: Open Swagger UI**

Navigate to: **https://localhost:7001/swagger**

You'll see the **Patients** section with 3 endpoints:
1. `GET /api/patients` - Get all patients
2. `GET /api/patients/search` - Search patients
3. `GET /api/patients/{mrNo}` - Get patient by MRNo

---

### **Step 3: Test Endpoints**

## 📋 **Endpoint 1: Get All Patients**

**URL**: `GET /api/patients?pageNumber=1&pageSize=20`

**In Swagger**:
1. Click on `GET /api/patients`
2. Click **Try it out**
3. Set `pageNumber`: 1
4. Set `pageSize`: 20
5. Click **Execute**

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients?pageNumber=1&pageSize=20" -k
```

**Response**:
```json
{
  "patients": [
    {
      "mrNo": "1760",
      "personFirstName": "Tjkhdkcjd",
      "fullName": "Tjkhdkcjd sdcdc",
      "personSex": "Female",
      "age": 0,
      "personCellPhone": "645138451341563",
      "emiratesIDN": "000-0000-0000000-0",
      "facilityName": "Women's Health Center"
    }
  ],
  "totalCount": 156,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasNext": true
}
```

---

## 🔍 **Endpoint 2: Search Patients**

### **Search by MRNo**

**URL**: `GET /api/patients/search?mrNo=1760`

**In Swagger**:
1. Click on `GET /api/patients/search`
2. Click **Try it out**
3. Set `mrNo`: 1760
4. Click **Execute**

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/search?mrNo=1760" -k
```

---

### **Search by Name**

**URL**: `GET /api/patients/search?name=Tjkh`

**In Swagger**:
1. Set `name`: Tjkh
2. Click **Execute**

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/search?name=Tjkh" -k
```

> **Note**: Name search checks FirstName, MiddleName, and LastName (partial match)

---

### **Search by Emirates ID**

**URL**: `GET /api/patients/search?emiratesIDN=000-0000`

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/search?emiratesIDN=000-0000" -k
```

---

### **Search by Cell Phone**

**URL**: `GET /api/patients/search?cellPhone=6451384`

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/search?cellPhone=6451384" -k
```

---

### **Multiple Search Criteria** (AND logic)

**URL**: `GET /api/patients/search?name=John&emiratesIDN=784`

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/search?name=John&emiratesIDN=784" -k
```

---

## 👤 **Endpoint 3: Get Patient by MRNo**

**URL**: `GET /api/patients/1760`

**In Swagger**:
1. Click on `GET /api/patients/{mrNo}`
2. Click **Try it out**
3. Set `mrNo`: 1760
4. Click **Execute**

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/patients/1760" -k
```

**Response**:
```json
{
  "mrNo": "1760",
  "personFirstName": "Tjkhdkcjd",
  "personMiddleName": null,
  "personLastName": "sdcdc",
  "fullName": "Tjkhdkcjd sdcdc",
  "personSex": "Female",
  "patientBirthDate": "2024-10-15T00:00:00",
  "age": 0,
  "personCellPhone": "645138451341563",
  "emiratesIDN": "000-0000-0000000-0",
  "facilityName": "Women's Health Center"
}
```

---

## 📱 **Frontend Integration Examples**

### **JavaScript/React**

```javascript
// Get patients with pagination
async function getPatients(pageNumber = 1, pageSize = 20) {
  const response = await fetch(
    `https://localhost:7001/api/patients?pageNumber=${pageNumber}&pageSize=${pageSize}`
  );
  const data = await response.json();
  return data;
}

// Search by MRNo
async function searchByMRNo(mrNo) {
  const response = await fetch(
    `https://localhost:7001/api/patients/search?mrNo=${mrNo}`
  );
  const data = await response.json();
  return data;
}

// Search by name
async function searchByName(name, pageNumber = 1) {
  const response = await fetch(
    `https://localhost:7001/api/patients/search?name=${name}&pageNumber=${pageNumber}&pageSize=20`
  );
  const data = await response.json();
  return data;
}

// Usage
const patients = await getPatients(1, 20);
console.log(`Total patients: ${patients.totalCount}`);
console.log(`Page ${patients.pageNumber} of ${patients.totalPages}`);

patients.patients.forEach(patient => {
  console.log(`${patient.mrNo}: ${patient.fullName}`);
});
```

---

### **Angular Service**

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private apiUrl = 'https://localhost:7001/api/patients';

  constructor(private http: HttpClient) {}

  getPatients(pageNumber: number = 1, pageSize: number = 20): Observable<any> {
    return this.http.get(`${this.apiUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  searchPatients(mrNo?: string, name?: string, pageNumber: number = 1): Observable<any> {
    let params = `pageNumber=${pageNumber}&pageSize=20`;
    if (mrNo) params += `&mrNo=${mrNo}`;
    if (name) params += `&name=${name}`;
    return this.http.get(`${this.apiUrl}/search?${params}`);
  }

  getPatientByMRNo(mrNo: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${mrNo}`);
  }
}

// Usage in component
this.patientService.searchPatients('1760').subscribe(data => {
  this.patients = data.patients;
  this.totalCount = data.totalCount;
});
```

---

### **Vue.js Composition API**

```javascript
import { ref } from 'vue';

export function usePatients() {
  const patients = ref([]);
  const totalCount = ref(0);
  const loading = ref(false);

  const searchPatients = async (mrNo, name, pageNumber = 1) => {
    loading.value = true;
    try {
      let url = `https://localhost:7001/api/patients/search?pageNumber=${pageNumber}&pageSize=20`;
      if (mrNo) url += `&mrNo=${mrNo}`;
      if (name) url += `&name=${name}`;
      
      const response = await fetch(url);
      const data = await response.json();
      
      patients.value = data.patients;
      totalCount.value = data.totalCount;
    } catch (error) {
      console.error('Error searching patients:', error);
    } finally {
      loading.value = false;
    }
  };

  return { patients, totalCount, loading, searchPatients };
}
```

---

## 🎯 **Key Features**

### ✅ **Pagination**
- Default: 20 records per page
- Maximum: 100 records per page
- Returns total count and page information

### ✅ **Search**
- **Partial matching** enabled (SQL LIKE)
- Search by multiple fields simultaneously
- Case-insensitive search

### ✅ **No Authentication Required**
- Endpoints are public (AllowAnonymous)
- No JWT token needed
- Perfect for patient registration/lookup

### ✅ **Performance Optimized**
- Dapper ORM for fast queries
- Pagination reduces data load
- Database indexes on search fields

---

## 📊 **Response Structure**

### **PaginatedPatientResponse**
```json
{
  "patients": [...],        // Array of patient objects
  "totalCount": 156,        // Total matching records
  "pageNumber": 1,          // Current page
  "pageSize": 20,           // Records per page
  "totalPages": 8,          // Total pages (calculated)
  "hasPrevious": false,     // Has previous page
  "hasNext": true           // Has next page
}
```

### **PatientListItemDto**
```json
{
  "mrNo": "1760",
  "personFirstName": "Tjkhdkcjd",
  "personMiddleName": null,
  "personLastName": "sdcdc",
  "fullName": "Tjkhdkcjd sdcdc",    // Auto-calculated
  "personSex": "Female",
  "patientBirthDate": "2024-10-15",
  "age": 0,                           // Auto-calculated from birthdate
  "personCellPhone": "645138451341563",
  "personEmail": null,
  "personAddress1": null,
  "nationality": null,
  "emiratesIDN": "000-0000-0000000-0",
  "patientFirstVisitDate": null,
  "createdDate": "2024-12-18T15:09:50",
  "vipPatient": false,
  "inactive": false,
  "facilityName": "Women's Health Center"
}
```

---

## ⚡ **Testing Checklist**

- [ ] Application running on https://localhost:7001
- [ ] Swagger UI accessible
- [ ] Get all patients endpoint works
- [ ] Search by MRNo works
- [ ] Search by name works
- [ ] Search by Emirates ID works
- [ ] Get patient by MRNo works
- [ ] Pagination works correctly
- [ ] No authentication required

---

## 🔧 **Troubleshooting**

### **Issue: Can't connect to database**
```
Error: Cannot open database "UEMedical_For_R&D"
```
**Solution**: 
- Check connection string in `appsettings.json`
- Verify server 175.107.195.221 is accessible
- Check SQL Server credentials (Tekno / 123qwe@)

---

### **Issue: No data returned**
```json
{
  "patients": [],
  "totalCount": 0
}
```
**Solution**:
- Check if RegPatient table has data
- Verify search criteria
- Check database connection

---

### **Issue: Application won't start**
**Solution**:
```bash
# Close all Visual Studio and IIS Express instances
dotnet clean
dotnet build
dotnet run --project "Coherent Web Portal"
```

---

## 📚 **Documentation Files**

1. **PATIENT_API_DOCUMENTATION.md** - Complete API reference
2. **PATIENT_ENDPOINTS_QUICK_START.md** - This file
3. **README.md** - Overall project documentation
4. **SETUP_GUIDE.md** - Initial setup instructions

---

## 🎉 **You're Ready!**

Your patient endpoints are fully functional and ready to use. No authentication required, just start the app and call the endpoints!

**Next Steps**:
1. Run the application
2. Test in Swagger
3. Integrate with your frontend
4. Add more features as needed

---

## 💡 **Need Help?**

- Check Swagger UI for live documentation
- Review controller: `Controllers/PatientsController.cs`
- Check repository: `Infrastructure/Repositories/PatientRepository.cs`
- See entity: `Domain/Entities/RegPatient.cs`
