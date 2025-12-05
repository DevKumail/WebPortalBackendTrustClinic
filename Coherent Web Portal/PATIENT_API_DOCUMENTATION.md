# Patient API Documentation

## Overview
Patient endpoints for searching and retrieving patient data from `RegPatient` table in `UEMedical_For_R&D` database.

**Authentication**: ❌ **NOT REQUIRED** - These endpoints are accessible without JWT token

---

## Base URL
- Development: `https://localhost:7001/api/patients`
- Production: `https://your-domain.com/api/patients`

---

## Endpoints

### 1. Get All Patients (Paginated)

**Endpoint**: `GET /api/patients`

**Description**: Retrieve all patients with pagination support.

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageNumber | int | No | 1 | Page number to retrieve |
| pageSize | int | No | 20 | Number of records per page (max: 100) |

**Example Request**:
```http
GET /api/patients?pageNumber=1&pageSize=20
```

**Success Response (200 OK)**:
```json
{
  "patients": [
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
  ],
  "totalCount": 156,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasPrevious": false,
  "hasNext": true
}
```

---

### 2. Search Patients

**Endpoint**: `GET /api/patients/search`

**Description**: Search patients by MRNo, name, Emirates ID, or cell phone with pagination.

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| mrNo | string | No | null | Medical Record Number (partial match) |
| name | string | No | null | First, middle, or last name (partial match) |
| emiratesIDN | string | No | null | Emirates ID Number (partial match) |
| cellPhone | string | No | null | Cell phone number (partial match) |
| pageNumber | int | No | 1 | Page number |
| pageSize | int | No | 20 | Records per page (max: 100) |

**Example Requests**:

**Search by MRNo**:
```http
GET /api/patients/search?mrNo=1760&pageNumber=1&pageSize=20
```

**Search by Name**:
```http
GET /api/patients/search?name=Tjkh&pageNumber=1&pageSize=20
```

**Search by Emirates ID**:
```http
GET /api/patients/search?emiratesIDN=000-0000&pageNumber=1&pageSize=20
```

**Search by Cell Phone**:
```http
GET /api/patients/search?cellPhone=6451384&pageNumber=1&pageSize=20
```

**Multiple Search Criteria** (AND logic):
```http
GET /api/patients/search?name=John&emiratesIDN=784&pageNumber=1&pageSize=20
```

**Success Response (200 OK)**:
```json
{
  "patients": [
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
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

**No Results Found (200 OK)**:
```json
{
  "patients": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 0,
  "hasPrevious": false,
  "hasNext": false
}
```

---

### 3. Get Patient by MRNo

**Endpoint**: `GET /api/patients/{mrNo}`

**Description**: Retrieve detailed patient information by Medical Record Number.

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| mrNo | string | Yes | Medical Record Number |

**Example Request**:
```http
GET /api/patients/1760
```

**Success Response (200 OK)**:
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

**Not Found (404)**:
```json
{
  "message": "Patient with MRNo 9999 not found"
}
```

---

## Response Fields

### PatientListItemDto

| Field | Type | Description |
|-------|------|-------------|
| mrNo | string | Medical Record Number |
| personFirstName | string | Patient's first name |
| personMiddleName | string | Patient's middle name |
| personLastName | string | Patient's last name |
| fullName | string | Computed: Full name (First Middle Last) |
| personSex | string | Gender (Male/Female) |
| patientBirthDate | datetime | Date of birth |
| age | int | Computed: Age in years |
| personCellPhone | string | Cell phone number |
| personEmail | string | Email address |
| personAddress1 | string | Primary address |
| nationality | string | Nationality |
| emiratesIDN | string | Emirates ID Number |
| patientFirstVisitDate | datetime | First visit date |
| createdDate | datetime | Record creation date |
| vipPatient | bool | VIP status flag |
| inactive | bool | Inactive status flag |
| facilityName | string | Associated facility name |

### PaginatedPatientResponse

| Field | Type | Description |
|-------|------|-------------|
| patients | array | Array of patient objects |
| totalCount | int | Total number of matching records |
| pageNumber | int | Current page number |
| pageSize | int | Records per page |
| totalPages | int | Computed: Total number of pages |
| hasPrevious | bool | Computed: Has previous page |
| hasNext | bool | Computed: Has next page |

---

## Error Responses

### 400 Bad Request
Invalid request parameters.

```json
{
  "message": "Invalid parameters"
}
```

### 500 Internal Server Error
Server error occurred.

```json
{
  "message": "An error occurred while searching patients"
}
```

---

## Usage Examples

### JavaScript/Fetch

```javascript
// Get all patients (first page)
const response = await fetch('https://localhost:7001/api/patients?pageNumber=1&pageSize=20');
const data = await response.json();
console.log(`Found ${data.totalCount} patients`);
console.log(data.patients);

// Search by MRNo
const searchResponse = await fetch('https://localhost:7001/api/patients/search?mrNo=1760');
const searchData = await searchResponse.json();

// Get specific patient
const patientResponse = await fetch('https://localhost:7001/api/patients/1760');
const patient = await patientResponse.json();
console.log(`Patient: ${patient.fullName}`);
```

### cURL

```bash
# Get all patients
curl "https://localhost:7001/api/patients?pageNumber=1&pageSize=20"

# Search by name
curl "https://localhost:7001/api/patients/search?name=John&pageNumber=1&pageSize=20"

# Get patient by MRNo
curl "https://localhost:7001/api/patients/1760"
```

### C# HttpClient

```csharp
using var client = new HttpClient();
client.BaseAddress = new Uri("https://localhost:7001");

// Search patients
var response = await client.GetAsync("/api/patients/search?name=John&pageNumber=1&pageSize=20");
var json = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<PaginatedPatientResponse>(json);

Console.WriteLine($"Found {result.TotalCount} patients");
foreach (var patient in result.Patients)
{
    Console.WriteLine($"{patient.MRNo}: {patient.FullName}");
}
```

### Python Requests

```python
import requests

# Search patients
url = "https://localhost:7001/api/patients/search"
params = {
    "name": "John",
    "pageNumber": 1,
    "pageSize": 20
}
response = requests.get(url, params=params)
data = response.json()

print(f"Found {data['totalCount']} patients")
for patient in data['patients']:
    print(f"{patient['mrNo']}: {patient['fullName']}")
```

---

## Performance Notes

- **Pagination**: Always use pagination for better performance
- **Page Size**: Maximum page size is 100 records
- **Search**: Partial matching is enabled (uses SQL LIKE)
- **Indexing**: Queries are optimized with proper database indexes
- **Caching**: Consider implementing caching for frequently accessed data

---

## Testing in Swagger

1. Navigate to: `https://localhost:7001/swagger`
2. Find **Patients** section
3. Expand endpoint you want to test
4. Click **Try it out**
5. Enter parameters
6. Click **Execute**
7. View response below

**No authentication required** - You can test immediately without logging in!

---

## Database Table

These endpoints query the `RegPatient` table in `UEMedical_For_R&D` database.

**Connection**: `Server=175.107.195.221;Database=UEMedical_For_R&D;User ID=Tekno;...`

---

## Next Steps

To extend this API, you can:

1. **Add More Filters**: Age range, gender, facility, etc.
2. **Add Sorting**: Sort by name, date, MRNo
3. **Add Statistics**: Patient count by facility, gender distribution
4. **Add Export**: Export to Excel/PDF
5. **Add Patient Details Endpoint**: Return full patient record with all fields

---

## Support

For questions or issues:
- Check Swagger documentation: `https://localhost:7001/swagger`
- Review main documentation: `README.md`
- Check implementation: `Controllers/PatientsController.cs`
