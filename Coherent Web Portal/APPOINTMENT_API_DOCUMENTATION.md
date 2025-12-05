# Appointment Management API Documentation

## Overview
Complete appointment management system with doctor scheduling, patient health records, and appointment booking capabilities.

**Authentication**: ❌ **NOT REQUIRED** - All endpoints accessible without JWT token for mobile app integration

---

## Database Architecture

### Primary Database: `UEMedical_For_R&D`
- Provider schedules (`ProviderSchedules`)
- Appointments (`SchAppointments`)
- HR Employees/Doctors (`HREmployees`)
- Patient data (`RegPatient`)
- Holiday schedules (`HolidaySchedules`)
- Blocked timeslots (`SchBlockTimeslots`)
- Vital signs, medications, allergies

### Secondary Database: `CoherentMobApp`
- Doctor profiles (`MDoctors`)
- Specialities (`MSpecility`)
- Facilities (`MFacility`)
- Doctor-Facility mappings (`MDoctorFacilities`)

---

## Base URL
- Development: `https://localhost:7001/api`
- Production: `https://your-domain.com/api`

---

## 📅 Appointment Management Endpoints

### 4.1.1 Get All Appointments by MRNO

**Endpoint**: `GET /api/Appointments/GetAllAppointmentByMRNO`

**Description**: Retrieve all upcoming and past appointments for a patient

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| MRNO | string | Yes | Medical Record Number |

**Example Request**:
```http
GET /api/Appointments/GetAllAppointmentByMRNO?MRNO=1007
```

**Success Response (200 OK)**:
```json
[
  {
    "appId": 123,
    "mrNo": "1007",
    "doctorId": 4,
    "doctorName": "Dr. Walid Reda Sayed",
    "speciality": "Reproductive Endocrinology",
    "siteId": 1,
    "siteName": "Main Clinic",
    "appointmentDate": "2025-12-10T00:00:00",
    "appointmentDateTime": "2025-12-10T10:00:00",
    "duration": 15,
    "status": "Scheduled",
    "reason": "Follow-up consultation",
    "notes": "Bring previous test results",
    "createdDate": "2025-12-05T14:30:00"
  },
  {
    "appId": 124,
    "mrNo": "1007",
    "doctorId": 5,
    "doctorName": "Dr. Muna Amam",
    "speciality": "Anesthesia",
    "siteId": 1,
    "appointmentDate": "2025-12-08T00:00:00",
    "appointmentDateTime": "2025-12-08T14:30:00",
    "duration": 15,
    "status": "Rescheduled",
    "reason": "Pre-operative assessment",
    "notes": null,
    "createdDate": "2025-12-03T09:15:00"
  }
]
```

**Status Values**:
- `Scheduled` - Appointment is confirmed
- `Rescheduled` - Appointment has been rescheduled
- `Cancelled` - Appointment was cancelled

---

### 4.1.2 Get Available Doctor Slots

**Endpoint**: `GET /api/Appointments/GetAvailableSlotOfDoctor`

**Description**: Get all available appointment slots for a doctor within a date range

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| doctorId | long | No* | Doctor ID from MDoctors table |
| prsnlAlias | string | No* | Doctor NPI/Alias |
| fromDate | DateTime | No | Start date (default: today) |
| toDate | DateTime | No | End date (default: +7 days) |

*Either `doctorId` OR `prsnlAlias` must be provided

**Example Requests**:

**By Doctor ID**:
```http
GET /api/Appointments/GetAvailableSlotOfDoctor?doctorId=4&fromDate=2025-12-10&toDate=2025-12-17
```

**By Doctor Alias/NPI**:
```http
GET /api/Appointments/GetAvailableSlotOfDoctor?prsnlAlias=GD10322&fromDate=2025-12-10&toDate=2025-12-17
```

**Success Response (200 OK)**:
```json
[
  {
    "specialityId": "1",
    "specialityName": "Reproductive Endocrinology",
    "facilityId": "1",
    "resourceCd": "4",
    "prsnlId": "4",
    "prsnlName": "Dr. Walid Reda Sayed",
    "resourceName": "Walid Sayed",
    "prsnlAlias": "GD10322",
    "execDttmFrom": "2025-12-05 16:40:00",
    "execDttmTo": "2025-12-05 16:40:00",
    "availableSlots": [
      {
        "slotId": "1000000",
        "dttmFrom": "2025-12-10 09:00:00",
        "dttmTo": "2025-12-10 09:15:00",
        "dttmDuration": "15",
        "slotState": "ACTIVE",
        "slotType": "F",
        "updtDttm": "2025-12-05 16:40:00"
      },
      {
        "slotId": "1000001",
        "dttmFrom": "2025-12-10 09:15:00",
        "dttmTo": "2025-12-10 09:30:00",
        "dttmDuration": "15",
        "slotState": "ACTIVE",
        "slotType": "F",
        "updtDttm": "2025-12-05 16:40:00"
      },
      {
        "slotId": "1000002",
        "dttmFrom": "2025-12-10 09:30:00",
        "dttmTo": "2025-12-10 09:45:00",
        "dttmDuration": "15",
        "slotState": "ACTIVE",
        "slotType": "F",
        "updtDttm": "2025-12-05 16:40:00"
      }
    ]
  }
]
```

**Slot Calculation Logic**:
- ✅ Reads doctor schedules from `ProviderSchedules`
- ✅ Excludes weekends (Saturday, Sunday)
- ✅ Excludes holidays from `HolidaySchedules`
- ✅ Excludes break times
- ✅ Excludes blocked timeslots
- ✅ Excludes existing appointments
- ✅ Generates 15-minute slots

---

### 4.1.3 Book Appointment

**Endpoint**: `POST /api/Appointments/BookAppointment`

**Description**: Book a new appointment for a patient

**Request Body**:
```json
{
  "doctorId": 4,
  "mrno": "1007",
  "appointmentDateTime": "2025-12-10T10:00:00",
  "day": "Tuesday",
  "reason": "Regular checkup",
  "notes": "Patient prefers morning slots"
}
```

**Request Fields**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| doctorId | long | Yes | Doctor ID |
| mrno | string | Yes | Patient Medical Record Number |
| appointmentDateTime | DateTime | Yes | Appointment date and time |
| day | string | No | Day of week |
| reason | string | No | Reason for visit |
| notes | string | No | Additional notes |

**Success Response (201 Created)**:
```json
{
  "message": "Appointment booked successfully",
  "appointmentId": 125,
  "status": "scheduled"
}
```

**Validation**:
- ✅ MRNO cannot be empty
- ✅ DoctorId must be valid
- ✅ Appointment cannot be in the past
- ✅ Slot must be available

**Error Responses**:

**400 Bad Request**:
```json
{
  "message": "MRNO is required"
}
```

**400 Bad Request**:
```json
{
  "message": "Appointment date/time cannot be in the past"
}
```

---

### 4.1.4 Modify Appointment

**Endpoint**: `POST/PUT /api/Appointments/ChangeBookedAppointment`

**Description**: Reschedule or cancel an existing appointment

**Request Body (Reschedule)**:
```json
{
  "appId": 125,
  "doctorId": 4,
  "mrno": "1007",
  "appointmentDateTime": "2025-12-11T14:00:00",
  "status": "rescheduled",
  "reason": "Patient requested different time",
  "notes": "Changed from 10:00 AM to 2:00 PM"
}
```

**Request Body (Cancel)**:
```json
{
  "appId": 125,
  "doctorId": 4,
  "mrno": "1007",
  "status": "cancel",
  "reason": "Patient unable to attend",
  "notes": "Will reschedule later"
}
```

**Request Fields**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| appId | long | Yes | Appointment ID |
| doctorId | long | Yes | Doctor ID |
| mrno | string | Yes | Patient MRNO |
| appointmentDateTime | DateTime | No* | New date/time (for reschedule) |
| status | string | Yes | "rescheduled" or "cancel" |
| reason | string | No | Reason for change |
| notes | string | No | Additional notes |

*Required when status is "rescheduled"

**Success Response (200 OK)**:
```json
{
  "message": "Appointment rescheduled successfully",
  "appointmentId": 125,
  "status": "rescheduled"
}
```

**Error Responses**:

**404 Not Found**:
```json
{
  "message": "Appointment not found"
}
```

**400 Bad Request**:
```json
{
  "message": "New appointment date/time is required for rescheduling"
}
```

---

## 👨‍⚕️ Doctor Management Endpoints

### 4.2 Get All Doctors

**Endpoint**: `GET /api/Doctors/GetAllDoctors`

**Description**: Retrieve full profiles of all active doctors

**Example Request**:
```http
GET /api/Doctors/GetAllDoctors
```

**Success Response (200 OK)**:
```json
[
  {
    "dId": 4,
    "doctorName": "Dr. Walid Reda Sayed",
    "arDoctorName": "د. وليد رضا سيد",
    "title": "Consultant Reproductive Endocrinologist & Infertility (IVF)",
    "arTitle": "استشاري غدد صماء التناسلية والعقم",
    "speciality": "Reproductive Endocrinology",
    "arSpeciality": "غدد صماء تناسلية",
    "yearsOfExperience": "30+",
    "nationality": "Germany",
    "languages": "Arabic,German,English",
    "doctorPhotoName": "dr_walid.jpg",
    "about": "Dr. Walid Reda Sayed is an accomplished medical professional...",
    "education": "M.B.B.S from Ein Shams University, Doctorate from Düsseldorf",
    "experience": "Over 30 years in gynecology, obstetrics, and reproductive medicine",
    "expertise": "IVF, Reproductive Medicine, Gynecological Endocrinology",
    "licenceNo": "UAE123",
    "gender": "M",
    "active": true,
    "facilities": [
      "Women's Health Center",
      "Main Hospital"
    ]
  },
  {
    "dId": 5,
    "doctorName": "Dr. Muna Amam",
    "arDoctorName": "د. منى أمام",
    "title": "Specialist Anesthesia",
    "arTitle": "أخصائي تخدير",
    "speciality": "Anesthesia",
    "yearsOfExperience": "17+",
    "nationality": "Syria",
    "languages": "Arabic,English",
    "doctorPhotoName": "dr_muna.jpg",
    "about": "Dr. Muna Amam a highly experienced Specialist...",
    "licenceNo": "UAE456",
    "gender": "F",
    "active": true,
    "facilities": [
      "Women's Health Center"
    ]
  }
]
```

**Response Fields**:
- `dId` - Doctor unique identifier
- `doctorName` - Doctor full name in English
- `arDoctorName` - Doctor name in Arabic
- `title` - Professional title/designation
- `speciality` - Medical speciality
- `yearsOfExperience` - Years of practice
- `nationality` - Doctor's nationality
- `languages` - Comma-separated languages spoken
- `doctorPhotoName` - Profile photo filename
- `about` - Biography
- `education` - Educational background
- `experience` - Work experience details
- `expertise` - Areas of expertise
- `licenceNo` - Medical license number
- `gender` - M/F
- `active` - Active status
- `facilities` - List of facilities where doctor practices

---

### Get Doctor by ID

**Endpoint**: `GET /api/Doctors/{doctorId}`

**Description**: Get detailed profile of a specific doctor

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| doctorId | int | Yes | Doctor ID |

**Example Request**:
```http
GET /api/Doctors/4
```

**Success Response**: Same structure as GetAllDoctors for single doctor

**Error Response (404)**:
```json
{
  "message": "Doctor with ID 999 not found"
}
```

---

## 🏥 Patient Health Endpoints

### 4.3 Get Vital Signs by MRNO

**Endpoint**: `GET /api/PatientHealth/GetVitalSignsByMRNO`

**Description**: Retrieve patient vital signs including BMI, weight, height, temperature

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| MRNO | string | Yes | Medical Record Number |

**Example Request**:
```http
GET /api/PatientHealth/GetVitalSignsByMRNO?MRNO=1007
```

**Success Response (200 OK)**:
```json
{
  "mrno": "1007",
  "weight": 75.5,
  "height": 175.0,
  "bmi": 24.65,
  "temperature": 36.8,
  "bloodPressure": "120/80",
  "heartRate": 72,
  "recordedDate": "2025-12-05T14:30:00"
}
```

**Field Descriptions**:
- `weight` - Weight in kilograms
- `height` - Height in centimeters
- `bmi` - Body Mass Index (auto-calculated)
- `temperature` - Body temperature in Celsius
- `bloodPressure` - Systolic/Diastolic (e.g., "120/80")
- `heartRate` - Heart rate in beats per minute
- `recordedDate` - Date of measurement

**BMI Calculation**:
```
BMI = weight(kg) / (height(m))²
```

**Error Response (404)**:
```json
{
  "message": "Vital signs not found for MRNO 9999"
}
```

---

### 4.4 Get Medications by MRNO

**Endpoint**: `GET /api/PatientHealth/GetMedicationsByMRNO`

**Description**: Retrieve medication list prescribed by doctors

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| MRNO | string | Yes | Medical Record Number |

**Example Request**:
```http
GET /api/PatientHealth/GetMedicationsByMRNO?MRNO=1007
```

**Success Response (200 OK)**:
```json
[
  {
    "medicationId": 1,
    "mrno": "1007",
    "medicationName": "Metformin",
    "dosage": "500mg",
    "frequency": "Twice daily",
    "route": "Oral",
    "prescribedBy": "Dr. Walid Reda Sayed",
    "prescribedDate": "2025-11-15T10:00:00",
    "startDate": "2025-11-15T00:00:00",
    "endDate": "2026-02-15T00:00:00",
    "instructions": "Take with meals",
    "isActive": true
  },
  {
    "medicationId": 2,
    "mrno": "1007",
    "medicationName": "Folic Acid",
    "dosage": "5mg",
    "frequency": "Once daily",
    "route": "Oral",
    "prescribedBy": "Dr. Walid Reda Sayed",
    "prescribedDate": "2025-11-15T10:00:00",
    "startDate": "2025-11-15T00:00:00",
    "endDate": null,
    "instructions": "Take in the morning",
    "isActive": true
  }
]
```

**Route Values**:
- `Oral` - By mouth
- `IV` - Intravenous
- `IM` - Intramuscular
- `Topical` - Applied to skin
- `Sublingual` - Under tongue

---

### 4.5 Get Allergies by MRNO

**Endpoint**: `GET /api/PatientHealth/GetAllergyByMRNO`

**Description**: Retrieve patient allergy history

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| MRNO | string | Yes | Medical Record Number |

**Example Request**:
```http
GET /api/PatientHealth/GetAllergyByMRNO?MRNO=1007
```

**Success Response (200 OK)**:
```json
[
  {
    "allergyId": 1,
    "mrno": "1007",
    "allergyType": "Drug",
    "allergen": "Penicillin",
    "reaction": "Rash, itching",
    "severity": "Moderate",
    "onsetDate": "2020-03-15T00:00:00",
    "notes": "Developed rash after taking antibiotics",
    "isActive": true
  },
  {
    "allergyId": 2,
    "mrno": "1007",
    "allergyType": "Food",
    "allergen": "Peanuts",
    "reaction": "Anaphylaxis",
    "severity": "Severe",
    "onsetDate": "2015-07-20T00:00:00",
    "notes": "Carry EpiPen at all times",
    "isActive": true
  }
]
```

**Allergy Types**:
- `Drug` - Medication allergies
- `Food` - Food allergies
- `Environmental` - Environmental allergies (pollen, dust, etc.)

**Severity Levels**:
- `Mild` - Minor reactions
- `Moderate` - Noticeable reactions
- `Severe` - Life-threatening reactions

---

## 📱 Frontend Integration Examples

### JavaScript/React

```javascript
// Get available slots
async function getAvailableSlots(doctorId, fromDate, toDate) {
  const response = await fetch(
    `https://localhost:7001/api/Appointments/GetAvailableSlotOfDoctor?doctorId=${doctorId}&fromDate=${fromDate}&toDate=${toDate}`
  );
  return await response.json();
}

// Book appointment
async function bookAppointment(appointmentData) {
  const response = await fetch(
    'https://localhost:7001/api/Appointments/BookAppointment',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(appointmentData)
    }
  );
  return await response.json();
}

// Get all doctors
async function getAllDoctors() {
  const response = await fetch('https://localhost:7001/api/Doctors/GetAllDoctors');
  return await response.json();
}

// Usage
const slots = await getAvailableSlots(4, '2025-12-10', '2025-12-17');
const doctors = await getAllDoctors();
```

---

### Angular Service

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private apiUrl = 'https://localhost:7001/api';

  constructor(private http: HttpClient) {}

  getAppointments(mrno: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/Appointments/GetAllAppointmentByMRNO?MRNO=${mrno}`);
  }

  getAvailableSlots(doctorId: number, fromDate: string, toDate: string): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/Appointments/GetAvailableSlotOfDoctor?doctorId=${doctorId}&fromDate=${fromDate}&toDate=${toDate}`
    );
  }

  bookAppointment(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointments/BookAppointment`, data);
  }

  modifyAppointment(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointments/ChangeBookedAppointment`, data);
  }

  getAllDoctors(): Observable<any> {
    return this.http.get(`${this.apiUrl}/Doctors/GetAllDoctors`);
  }

  getVitalSigns(mrno: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/PatientHealth/GetVitalSignsByMRNO?MRNO=${mrno}`);
  }
}
```

---

## 🧪 Testing in Swagger

1. Navigate to: `https://localhost:7001/swagger`
2. Find **Appointments**, **Doctors**, or **PatientHealth** sections
3. Expand the endpoint you want to test
4. Click **Try it out**
5. Enter required parameters
6. Click **Execute**
7. View response

**No authentication required** - Test immediately!

---

## ⚙️ Technical Implementation

### Database Connections
- **Primary Database**: `UEMedical_For_R&D` - Appointments, schedules, patient data
- **Secondary Database**: `CoherentMobApp` - Doctor profiles, facilities

### Slot Calculation Algorithm
1. Fetch doctor schedule from `ProviderSchedules`
2. Generate 15-minute time slots
3. Remove weekends
4. Remove holidays from `HolidaySchedules`
5. Remove break times
6. Remove blocked times from `SchBlockTimeslots`
7. Remove existing appointments from `SchAppointments`
8. Return available slots

### Date Format Handling
- Input format: `YYYYMMDDHHMMSS` (e.g., "20251210143000")
- API format: `yyyy-MM-dd HH:mm:ss`
- Auto-conversion handled by `DateStringConversion` helper

---

## 🔄 Error Handling

All endpoints return standard error responses:

**400 Bad Request**:
```json
{
  "message": "Descriptive error message"
}
```

**404 Not Found**:
```json
{
  "message": "Resource not found message"
}
```

**500 Internal Server Error**:
```json
{
  "message": "An error occurred while processing request"
}
```

---

## 📊 Database Tables Used

### UEMedical_For_R&D:
- `ProviderSchedules` - Doctor schedules
- `SchAppointments` - Appointments
- `HREmployees` - Healthcare providers
- `HolidaySchedules` - Holiday calendar
- `SchBlockTimeslots` - Blocked time slots
- `RegPatient` - Patient information

### CoherentMobApp:
- `MDoctors` - Doctor profiles
- `MSpecility` - Medical specialities
- `MFacility` - Healthcare facilities
- `MDoctorFacilities` - Doctor-facility mapping

---

## ✅ Implementation Checklist

- [x] Get all appointments by MRNO
- [x] Get available doctor slots with complex calculation
- [x] Book appointment
- [x] Modify appointment (reschedule/cancel)
- [x] Get all doctors
- [x] Get doctor by ID
- [x] Get vital signs by MRNO
- [x] Get medications by MRNO
- [x] Get allergies by MRNO
- [x] No authentication required
- [x] CORS configured for mobile app
- [x] Swagger documentation enabled
- [x] Error handling implemented
- [x] Logging implemented

---

## 🚀 Quick Start

1. **Run the application**:
   ```bash
   dotnet run --project "Coherent Web Portal"
   ```

2. **Test in Swagger**: https://localhost:7001/swagger

3. **Example API Call**:
   ```bash
   curl -X GET "https://localhost:7001/api/Doctors/GetAllDoctors" -k
   ```

---

## 📞 Support

For issues or questions:
- Check Swagger UI: https://localhost:7001/swagger
- Review code: `Controllers/AppointmentsController.cs`, `Controllers/DoctorsController.cs`
- Check logs: `logs/coherent-web-portal-*.txt`
