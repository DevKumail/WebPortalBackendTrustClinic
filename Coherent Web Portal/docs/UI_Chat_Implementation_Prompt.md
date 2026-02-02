# Chat Module UI Implementation Guide

## Overview
Implement a real-time chat system for the CRM web portal that supports:
1. **Doctor-Patient Chat** - One-to-one messaging between doctors and patients
2. **Staff Broadcast Channels** - Patients can message staff types (Nurse, Receptionist, IVFLab) and all staff of that type can see/respond

---

## Authentication
- All API calls require JWT token in `Authorization: Bearer <token>` header
- Login response includes `empId` and `empType` for staff identification

---

## API Endpoints

### Base URL: `/api/v1/crm-chat`

### 1. Doctor-Patient Chat

#### Get or Create Thread
```http
POST /api/v1/crm-chat/threads/get-or-create
Content-Type: application/json

{
  "patientMrNo": "MR001",
  "doctorLicenseNo": "DOC123"
}
```

**Response:**
```json
{
  "crmThreadId": "CRM-TH-123",
  "isNew": true
}
```

#### Send Message (Doctor to Patient)
```http
POST /api/v1/crm-chat/threads/{crmThreadId}/messages
Content-Type: application/json

{
  "senderType": "Doctor",
  "senderDoctorLicenseNo": "DOC123",
  "receiverType": "Patient",
  "receiverMrNo": "MR001",
  "messageType": "text",
  "content": "Hello, how are you feeling today?",
  "clientMessageId": "uuid-for-idempotency"
}
```

#### Get Conversations List
```http
GET /api/v1/crm-chat/conversations?doctorLicenseNo=DOC123&limit=50
```

**Response:**
```json
{
  "conversations": [
    {
      "crmThreadId": "CRM-TH-123",
      "counterpart": {
        "mrNo": "MR001",
        "patientName": "John Doe",
        "doctorLicenseNo": null,
        "doctorName": null,
        "doctorPhotoName": null
      },
      "lastMessage": {
        "content": "Thank you doctor",
        "messageType": "text",
        "sentAt": "2026-02-02T18:00:00Z",
        "senderType": "Patient"
      },
      "unreadCount": 2
    }
  ]
}
```

#### Get Unread Summary
```http
GET /api/v1/crm-chat/unread-summary?doctorLicenseNo=DOC123
```

#### Get Thread Messages
```http
GET /api/v1/crm-chat/threads/{crmThreadId}/messages?take=50
```

#### Mark Thread as Read
```http
POST /api/v1/crm-chat/threads/{crmThreadId}/mark-read?doctorLicenseNo=DOC123
```

---

### 2. Staff Broadcast Channels (Nurse/Receptionist/IVFLab)

#### Get or Create Broadcast Channel
```http
POST /api/v1/crm-chat/broadcast-channels/get-or-create
Content-Type: application/json

{
  "patientMrNo": "MR001",
  "staffType": "Nurse"  // Options: "Nurse", "Receptionist", "IVFLab"
}
```

**Response:**
```json
{
  "crmThreadId": "CRM-TH-456",
  "channelType": "Broadcast",
  "staffType": "Nurse",
  "isNew": true
}
```

#### Get Broadcast Channels for Staff Type
```http
GET /api/v1/crm-chat/broadcast-channels?staffType=Nurse&limit=50
```

**Response:**
```json
[
  {
    "crmThreadId": "CRM-TH-456",
    "patientMrNo": "MR001",
    "patientName": "John Doe",
    "staffType": "Nurse",
    "lastMessageContent": "I need help with...",
    "lastMessageAt": "2026-02-02T18:00:00Z",
    "unreadCount": 3
  }
]
```

#### Get Staff Unread Summary
```http
GET /api/v1/crm-chat/broadcast-channels/unread-summary?staffType=Nurse
```

**Response:**
```json
{
  "staffType": "Nurse",
  "totalUnreadCount": 15,
  "channelsWithUnread": 5
}
```

#### Send Message (Staff to Patient)
```http
POST /api/v1/crm-chat/broadcast-channels/{crmThreadId}/messages
Content-Type: application/json

{
  "senderEmpId": 123,
  "senderEmpType": 2,
  "receiverMrNo": "MR001",
  "messageType": "text",
  "content": "Hello, I'm the nurse on duty. How can I help?"
}
```

#### Mark Broadcast Channel as Read
```http
POST /api/v1/crm-chat/broadcast-channels/{crmThreadId}/mark-read?empId=123&staffType=Nurse
```

---

## Real-Time Updates (SignalR)

### Hub URL: `/hubs/crm-chat`

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/crm-chat", {
        accessTokenFactory: () => localStorage.getItem("token")
    })
    .withAutomaticReconnect()
    .build();

await connection.start();
```

### Join Thread (to receive messages)
```javascript
await connection.invoke("JoinThread", "CRM-TH-123");
```

### Leave Thread
```javascript
await connection.invoke("LeaveThread", "CRM-TH-123");
```

### Listen for New Messages
```javascript
connection.on("chat.message.created", (message) => {
    console.log("New message:", message);
    // {
    //   crmThreadId: "CRM-TH-123",
    //   crmMessageId: "CRM-MSG-456",
    //   senderType: "Doctor" | "Patient" | "Staff",
    //   senderDoctorLicenseNo: "DOC123",
    //   senderEmpId: 123,
    //   senderEmpType: 2,
    //   receiverType: "Patient",
    //   receiverMrNo: "MR001",
    //   messageType: "text",
    //   content: "Hello",
    //   sentAt: "2026-02-02T18:00:00Z"
    // }
});
```

### Listen for Read Receipts
```javascript
connection.on("chat.thread.read", (data) => {
    console.log("Thread marked as read:", data);
    // { crmThreadId, doctorLicenseNo OR empId, readAtUtc }
});
```

---

## CRM User Management

### Base URL: `/api/v1/crm-users`

#### Get CRM Users (for User Management screen)
```http
GET /api/v1/crm-users?empType=2&isCRM=true&limit=100
```

**Query Parameters:**
- `empType` (optional): Filter by employee type (1=Doctor, 2=Nurse, 3=Receptionist, 4=IVFLab, 5=Admin)
- `isCRM` (optional): Filter by CRM access (true/false)
- `limit` (optional): Max results (default 100)

**Response:**
```json
{
  "users": [
    {
      "empId": 123,
      "fullName": "Jane Smith",
      "fName": "Jane",
      "lName": "Smith",
      "email": "jane@hospital.com",
      "phone": "123456789",
      "userName": "jsmith",
      "empType": 2,
      "empTypeName": "Nurse",
      "speciality": null,
      "departmentID": 5,
      "isCRM": true,
      "active": true
    }
  ],
  "totalCount": 25
}
```

#### Toggle IsCRM for Single User
```http
PUT /api/v1/crm-users/{empId}/is-crm
Content-Type: application/json

{
  "isCRM": true
}
```

**Response:**
```json
{
  "success": true,
  "message": "IsCRM updated successfully",
  "affectedCount": 1
}
```

#### Bulk Update IsCRM
```http
POST /api/v1/crm-users/bulk-update-is-crm
Content-Type: application/json

{
  "empIds": [123, 456, 789],
  "isCRM": true
}
```

#### Get Employee Types (for dropdown)
```http
GET /api/v1/crm-users/emp-types
```

**Response:**
```json
[
  { "id": 1, "name": "Doctor/Provider" },
  { "id": 2, "name": "Nurse" },
  { "id": 3, "name": "Receptionist" },
  { "id": 4, "name": "IVFLab" },
  { "id": 5, "name": "Admin" }
]
```

---

## UI Screens to Build

### 1. Chat List Screen
- Show list of conversations for logged-in doctor/staff
- Display patient name, last message, unread count, timestamp
- Real-time updates via SignalR
- Search/filter capability
- Tab separation for Doctor chats vs Staff broadcast channels

### 2. Chat Detail Screen
- Message thread with sender/receiver bubbles
- Text input with send button
- File attachment support (future)
- Auto-scroll to latest message
- Mark as read when opened
- Real-time message updates

### 3. User Management Screen (Admin)
- Table of all HR employees
- Filter by EmpType dropdown
- Filter by IsCRM status
- Toggle IsCRM checkbox/switch per user
- Bulk select and update
- Search by name/username

---

## Staff Types Reference

| EmpType | Name | Can use Chat? |
|---------|------|---------------|
| 1 | Doctor/Provider | Yes (Doctor-Patient) |
| 2 | Nurse | Yes (Broadcast Channel) |
| 3 | Receptionist | Yes (Broadcast Channel) |
| 4 | IVFLab | Yes (Broadcast Channel) |
| 5 | Admin | Admin only |

---

## Message Types

| Type | Description |
|------|-------------|
| `text` | Plain text message |
| `image` | Image attachment (future) |
| `file` | File attachment (future) |
| `audio` | Voice message (future) |

---

## Notes

1. **IsCRM Flag**: Only users with `IsCRM = true` should have access to chat features
2. **EmpType in JWT**: Login response includes `empType` claim - use to determine which chat UI to show
3. **Broadcast Channels**: When patient messages "Nurse", ALL nurses see it. Any nurse can respond.
4. **Idempotency**: Use `clientMessageId` to prevent duplicate messages on retry
5. **Pagination**: Use `take`/`limit` parameters for large lists
