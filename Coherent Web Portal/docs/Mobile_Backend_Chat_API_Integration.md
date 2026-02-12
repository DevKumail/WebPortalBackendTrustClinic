# Mobile Backend - Chat API Integration Guide

## Overview

This document describes the new Chat APIs and changes to existing APIs that need to be integrated into the Mobile Backend. The Web Portal now supports:

1. **Doctor-Patient Chat** (existing, with updates)
2. **Staff Broadcast Channels** (NEW) - Patients can message staff types (Nurse, Receptionist, IVFLab)

---

## 🔴 BREAKING CHANGES

### 1. `ChatSendMessageRequest` - New Fields Added

The message sending request now includes additional fields for staff identification:

```json
{
  "crmThreadId": "CRM-TH-123",
  "clientMessageId": "uuid-for-idempotency",
  
  // Sender identification (one of these sets)
  "senderType": "Patient | Doctor | Staff",
  "senderMrNo": "MR001",              // If Patient
  "senderDoctorLicenseNo": "DOC123",  // If Doctor
  "senderEmpId": 123,                 // NEW: If Staff
  "senderEmpType": 2,                 // NEW: If Staff (1=Doctor, 2=Nurse, 3=Receptionist, 4=IVFLab)
  
  // Receiver identification (one of these sets)
  "receiverType": "Patient | Doctor | Staff",
  "receiverMrNo": "MR001",            // If Patient
  "receiverDoctorLicenseNo": "DOC123", // If Doctor
  "receiverStaffType": "Nurse",       // NEW: If Staff broadcast channel
  
  // Message content
  "messageType": "text",
  "content": "Hello",
  "fileUrl": null,
  "fileName": null,
  "fileSize": null,
  "sentAt": "2026-02-03T10:00:00Z"
}
```

### 2. Webhook Payload - New Staff Message Webhook

When staff sends message to patient, a new webhook is triggered:

```json
{
  "crmThreadId": "CRM-TH-456",
  "crmMessageId": "CRM-MSG-789",
  "staffType": "Nurse",
  "senderEmpId": 123,
  "patientMrNo": "MR001",
  "messageType": "text",
  "content": "Hello, I'm the nurse on duty",
  "fileUrl": null,
  "fileName": null,
  "fileSize": null,
  "sentAt": "2026-02-03T10:00:00Z"
}
```

---

## 🆕 NEW APIs - Staff Broadcast Channels

### Base URL: `/api/v2/chat`

All APIs use `[ThirdPartyAuth]` - require `X-Client-ID` and `X-Security-Key` headers.

---

### 1. Create/Get Broadcast Channel

Patient creates a channel to message a staff type. All staff of that type can see messages.

```http
POST /api/v2/chat/broadcast-channels/get-or-create
Content-Type: application/json
X-Client-ID: your-client-id
X-Security-Key: your-security-key

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

**Use Case:** When patient wants to contact nurses/receptionist/lab staff.

---

### 2. Get Broadcast Channels for Staff Type

Get all broadcast channels for a specific staff type (e.g., all Nurse channels).

```http
GET /api/v2/chat/broadcast-channels?staffType=Nurse&limit=50
X-Client-ID: your-client-id
X-Security-Key: your-security-key
```

**Response:**
```json
[
  {
    "crmThreadId": "CRM-TH-456",
    "patientMrNo": "MR001",
    "patientName": "John Doe",
    "staffType": "Nurse",
    "lastMessageContent": "I need help with my medication",
    "lastMessageAt": "2026-02-03T10:00:00Z",
    "unreadCount": 3
  },
  {
    "crmThreadId": "CRM-TH-789",
    "patientMrNo": "MR002",
    "patientName": "Jane Smith",
    "staffType": "Nurse",
    "lastMessageContent": "When is my next appointment?",
    "lastMessageAt": "2026-02-03T09:30:00Z",
    "unreadCount": 1
  }
]
```

**Use Case:** Staff dashboard showing all patient conversations.

---

### 3. Get Staff Unread Summary

Get total unread count for a staff type.

```http
GET /api/v2/chat/broadcast-channels/unread-summary?staffType=Nurse
X-Client-ID: your-client-id
X-Security-Key: your-security-key
```

**Response:**
```json
{
  "staffType": "Nurse",
  "totalUnreadCount": 15,
  "channelsWithUnread": 5
}
```

**Use Case:** Badge count on staff dashboard.

---

### 4. Get Thread Messages

Get messages for a specific thread (works for both doctor-patient and broadcast channels).

```http
GET /api/v2/chat/threads/{crmThreadId}/messages?take=50
X-Client-ID: your-client-id
X-Security-Key: your-security-key
```

**Response:**
```json
[
  {
    "crmMessageId": "CRM-MSG-001",
    "senderType": "Patient",
    "senderMrNo": "MR001",
    "senderDoctorLicenseNo": null,
    "senderEmpId": null,
    "senderEmpType": null,
    "messageType": "text",
    "content": "I need help",
    "fileUrl": null,
    "sentAt": "2026-02-03T09:00:00Z"
  },
  {
    "crmMessageId": "CRM-MSG-002",
    "senderType": "Staff",
    "senderMrNo": null,
    "senderDoctorLicenseNo": null,
    "senderEmpId": 123,
    "senderEmpType": 2,
    "messageType": "text",
    "content": "Hello, how can I help you?",
    "fileUrl": null,
    "sentAt": "2026-02-03T09:05:00Z"
  }
]
```

---

### 5. Mark Broadcast Channel as Read

Mark a broadcast channel as read for a specific staff member.

```http
POST /api/v2/chat/broadcast-channels/{crmThreadId}/mark-read?empId=123&staffType=Nurse
X-Client-ID: your-client-id
X-Security-Key: your-security-key
```

**Response:**
```json
{
  "success": true,
  "messagesMarked": 5
}
```

---

## 📝 EXISTING APIs - Updates Required

### 1. Send Message API

The existing send message API now supports staff senders:

```http
POST /api/v2/chat/messages
Content-Type: application/json
X-Client-ID: your-client-id
X-Security-Key: your-security-key

{
  "crmThreadId": "CRM-TH-123",
  "clientMessageId": "unique-guid",
  
  // For Staff sending to Patient:
  "senderType": "Staff",
  "senderEmpId": 123,
  "senderEmpType": 2,
  "receiverType": "Patient",
  "receiverMrNo": "MR001",
  
  "messageType": "text",
  "content": "Your test results are ready"
}
```

### 2. SignalR Events - New Fields

The `chat.message.created` event now includes staff fields:

```javascript
connection.on("chat.message.created", (message) => {
    // New fields added:
    // - senderEmpId (long?) - Staff employee ID
    // - senderEmpType (int?) - Staff type (1=Doctor, 2=Nurse, etc.)
    // - receiverStaffType (string?) - Target staff type for broadcast
});
```

---

## 🔐 Authentication

### JWT Token Changes

Login response now includes `empType` for staff identification:

```json
{
  "isSuccess": true,
  "accessToken": "eyJ...",
  "user": {
    "empId": 123,
    "empType": 2,        // NEW: 1=Doctor, 2=Nurse, 3=Receptionist, 4=IVFLab, 5=Admin
    "username": "nurse1",
    "firstName": "Jane",
    "lastName": "Smith",
    "roles": ["Nurse"],
    "permissions": [...]
  }
}
```

JWT token now contains `EmpType` claim.

---

## 📊 Staff Types Reference

| EmpType | Name | Description |
|---------|------|-------------|
| 1 | Doctor/Provider | Uses Doctor-Patient chat |
| 2 | Nurse | Uses Broadcast channels |
| 3 | Receptionist | Uses Broadcast channels |
| 4 | IVFLab | Uses Broadcast channels |
| 5 | Admin | Admin only |

---

## 🔄 Data Flow

### Patient → Staff (Broadcast)

```
1. Patient calls: POST /broadcast-channels/get-or-create
   - Creates channel with ConversationType = 'Support'
   
2. Patient sends message: POST /messages
   - Message stored with receiverStaffType = "Nurse"
   
3. ALL nurses see the message in their channel list
   - GET /broadcast-channels?staffType=Nurse
   
4. Any nurse can respond: POST /messages
   - senderType = "Staff", senderEmpId = 123, senderEmpType = 2
   
5. Webhook sent to mobile backend for push notification
```

### Staff → Patient

```
1. Staff gets channels: GET /broadcast-channels?staffType=Nurse
2. Staff opens channel: GET /threads/{crmThreadId}/messages
3. Staff sends reply: POST /messages (with senderEmpId, senderEmpType)
4. Webhook triggers push to patient
5. Staff marks read: POST /broadcast-channels/{crmThreadId}/mark-read
```

---

## ✅ Implementation Checklist

### Mobile Backend Changes:

- [ ] Update `ChatSendMessageRequest` model with new fields:
  - `senderEmpId` (long?)
  - `senderEmpType` (int?)
  - `receiverStaffType` (string?)

- [ ] Add new API endpoints:
  - [ ] `POST /broadcast-channels/get-or-create`
  - [ ] `GET /broadcast-channels`
  - [ ] `GET /broadcast-channels/unread-summary`
  - [ ] `GET /threads/{crmThreadId}/messages`
  - [ ] `POST /broadcast-channels/{crmThreadId}/mark-read`

- [ ] Update webhook handler for new `ChatStaffMessageCreatedWebhook`

- [ ] Update SignalR event handlers for new fields

- [ ] Update JWT token parsing for `EmpType` claim

- [ ] Add staff type logic in app (show different UI based on EmpType)

---

## 📞 Contact

For questions about these APIs, contact the Web Portal backend team.
