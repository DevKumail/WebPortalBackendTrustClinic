# Promotions Module - CRM UI Development Guide

## Overview
Build a Promotions management interface in CRM (Coherent HIS) that allows administrators to:
1. Create/Edit/Delete promotional banners
2. Upload images for mobile app slider
3. Set URLs for banner clicks
4. Control display order and schedule

**Base URL:** `{{API_BASE_URL}}/api/v1/promotions`

**Authentication:** JWT Bearer Token in Header
```
Authorization: Bearer {{access_token}}
```

---

## UI Structure

### Promotions Page
A single page to manage promotional banners/sliders for the mobile app.

---

## API Endpoints & Payloads

---

## 📋 PROMOTIONS MANAGEMENT

### 1. GET All Promotions
**Endpoint:** `GET /api/v1/promotions`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| isActive | boolean | No | Filter by active status |

**Response:**
```json
[
  {
    "promotionId": 1,
    "title": "Summer Sale",
    "arTitle": "تخفيضات الصيف",
    "imageFileName": "promo_1_abc.jpg",
    "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
    "linkUrl": "https://example.com/summer-sale",
    "linkType": "External",
    "displayOrder": 0,
    "startDate": "2026-01-01T00:00:00",
    "endDate": "2026-01-31T23:59:59",
    "isActive": true
  },
  {
    "promotionId": 2,
    "title": "New Services",
    "arTitle": "خدمات جديدة",
    "imageFileName": "promo_2_def.jpg",
    "imageUrl": "https://domain.com/images/promotions/promo_2_def.jpg",
    "linkUrl": "/services/fertility",
    "linkType": "Internal",
    "displayOrder": 1,
    "startDate": null,
    "endDate": null,
    "isActive": true
  }
]
```

---

### 2. GET Promotion by ID
**Endpoint:** `GET /api/v1/promotions/{promotionId}`

**Response:**
```json
{
  "promotionId": 1,
  "title": "Summer Sale",
  "arTitle": "تخفيضات الصيف",
  "description": "Get 20% off on all services",
  "arDescription": "احصل على خصم 20% على جميع الخدمات",
  "imageFileName": "promo_1_abc.jpg",
  "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
  "linkUrl": "https://example.com/summer-sale",
  "linkType": "External",
  "displayOrder": 0,
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-01-31T23:59:59",
  "isActive": true,
  "createdAt": "2026-01-01T10:00:00",
  "createdBy": 1,
  "updatedAt": "2026-01-15T14:30:00",
  "updatedBy": 1
}
```

---

### 3. CREATE Promotion
**Endpoint:** `POST /api/v1/promotions`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| title | string | Yes | Title (English) |
| arTitle | string | No | Title (Arabic) |
| description | string | No | Description (English) |
| arDescription | string | No | Description (Arabic) |
| imageFile | File | Yes | Banner image (jpg, jpeg, png, webp, gif - max 5MB) |
| linkUrl | string | No | URL to navigate when clicked |
| linkType | string | No | Internal, External, or None |
| displayOrder | int | No | Order in slider (default: 0) |
| startDate | datetime | No | When to start showing |
| endDate | datetime | No | When to stop showing |
| isActive | boolean | No | Active status (default: true) |

**Response:**
```json
{
  "promotionId": 1,
  "message": "Promotion created successfully",
  "promotion": {
    "promotionId": 1,
    "title": "Summer Sale",
    "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
    ...
  }
}
```

---

### 4. UPDATE Promotion
**Endpoint:** `PUT /api/v1/promotions/{promotionId}`

**Content-Type:** `multipart/form-data`

**Form Fields:** Same as CREATE (imageFile optional for update)

**Response:**
```json
{
  "promotionId": 1,
  "message": "Promotion updated successfully",
  "promotion": { ... }
}
```

---

### 5. Upload/Replace Image
**Endpoint:** `POST /api/v1/promotions/{promotionId}/image`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | Banner image (max 5MB) |

**Response:**
```json
{
  "promotionId": 1,
  "imageFileName": "promo_1_xyz.jpg",
  "imageUrl": "https://domain.com/images/promotions/promo_1_xyz.jpg",
  "message": "Image uploaded successfully"
}
```

---

### 6. Toggle Active Status
**Endpoint:** `PATCH /api/v1/promotions/{promotionId}/toggle-active?isActive=true`

**Response:**
```json
{
  "promotionId": 1,
  "isActive": true,
  "message": "Promotion activated successfully"
}
```

---

### 7. Update Display Order
**Endpoint:** `PATCH /api/v1/promotions/{promotionId}/order?displayOrder=2`

**Response:**
```json
{
  "promotionId": 1,
  "displayOrder": 2,
  "message": "Display order updated successfully"
}
```

---

### 8. DELETE Promotion
**Endpoint:** `DELETE /api/v1/promotions/{promotionId}`

**Response:**
```json
{
  "promotionId": 1,
  "message": "Promotion deleted successfully"
}
```

---

## 📱 MOBILE APP ENDPOINT

### 9. GET Active Promotions (Slider)
**Endpoint:** `GET /api/v1/promotions/slider`

**Authentication:** None (Public endpoint)

**Description:** Returns only active promotions within their date range, ordered by displayOrder.

**Response:**
```json
[
  {
    "promotionId": 1,
    "title": "Summer Sale",
    "arTitle": "تخفيضات الصيف",
    "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
    "linkUrl": "https://example.com/summer-sale",
    "linkType": "External",
    "displayOrder": 0
  }
]
```

---

## 🎨 UI/UX Recommendations

### Promotions Page Layout
1. **Data Table** with columns:
   - Thumbnail (small preview)
   - Title (EN/AR)
   - Link URL
   - Display Order
   - Schedule (Start/End Date)
   - Status (Active/Inactive toggle)
   - Actions (Edit/Delete)

2. **Add Promotion** button opens a modal/form

### Promotion Form (Add/Edit)
1. **Image Upload** - Large preview area with drag & drop
2. **Title** fields (EN/AR)
3. **Description** fields (EN/AR) - optional
4. **Link Settings:**
   - Link Type dropdown: None, Internal, External
   - Link URL input (show only if type != None)
5. **Schedule:**
   - Start Date picker (optional)
   - End Date picker (optional)
6. **Display Order** - Number input
7. **Active** toggle

### Link Type Logic
| LinkType | Description | Example |
|----------|-------------|---------|
| None | No action on click | Just display banner |
| Internal | Navigate within app | `/services/fertility` |
| External | Open in browser | `https://example.com` |

### Drag & Drop Reorder
Allow users to drag & drop rows to reorder promotions. Call the `PATCH /order` endpoint on drop.

---

## 📋 Permissions Required
- `Promotions.Read` - View promotions
- `Promotions.Manage` - Create/Update/Delete promotions

---

## File Storage
| Type | Path | Max Size |
|------|------|----------|
| Promotion Images | `/images/promotions/` | 5MB |

---

## Allowed Image Extensions
- .jpg
- .jpeg
- .png
- .webp
- .gif

---

## Best Practices
1. **Image Dimensions:** Recommend 1200x600 pixels (2:1 ratio) for mobile slider
2. **File Size:** Keep images under 500KB for faster loading
3. **Schedule:** Use start/end dates for time-limited promotions
4. **Order:** Lower displayOrder values appear first in slider
