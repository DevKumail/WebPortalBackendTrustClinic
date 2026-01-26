# Mobile App API v2 Documentation

## Overview
Mobile backend APIs for Coherent HIS Patient Mobile App.

**Base URL:** `{{API_BASE_URL}}/api/v2`

**Authentication:** JWT Bearer Token (for protected endpoints)
```
Authorization: Bearer {{access_token}}
```

---

# 📢 PROMOTIONS (Slider)

## 1. Get Promotions Slider
**Endpoint:** `GET /api/v2/promotions/slider`

**Authentication:** None (Public)

**Description:** Returns active promotions for home screen slider. Only shows promotions within their scheduled date range.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "promotionId": 1,
      "title": "Summer Sale",
      "arTitle": "تخفيضات الصيف",
      "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
      "linkUrl": "https://example.com/summer-sale",
      "linkType": "External",
      "displayOrder": 0
    },
    {
      "promotionId": 2,
      "title": "New Fertility Services",
      "arTitle": "خدمات الخصوبة الجديدة",
      "imageUrl": "https://domain.com/images/promotions/promo_2_def.jpg",
      "linkUrl": "/services/fertility",
      "linkType": "Internal",
      "displayOrder": 1
    }
  ],
  "count": 2
}
```

### Link Type Handling
| LinkType | Action |
|----------|--------|
| `None` | No action on tap |
| `Internal` | Navigate within app (use linkUrl as route) |
| `External` | Open in WebView or browser |

---

## 2. Get Promotion Detail
**Endpoint:** `GET /api/v2/promotions/{promotionId}`

**Authentication:** None (Public)

**Response:**
```json
{
  "success": true,
  "data": {
    "promotionId": 1,
    "title": "Summer Sale",
    "arTitle": "تخفيضات الصيف",
    "description": "Get 20% off on all services",
    "arDescription": "احصل على خصم 20% على جميع الخدمات",
    "imageUrl": "https://domain.com/images/promotions/promo_1_abc.jpg",
    "linkUrl": "https://example.com/summer-sale",
    "linkType": "External",
    "displayOrder": 0,
    "startDate": "2026-01-01T00:00:00",
    "endDate": "2026-01-31T23:59:59",
    "isActive": true
  }
}
```

---

# 📚 PATIENT EDUCATION

## 3. Get Education Categories
**Endpoint:** `GET /api/v2/patient-education/categories`

**Authentication:** None (Public)

**Description:** Returns all active education categories with education count.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "categoryId": 1,
      "categoryName": "Fertility",
      "arCategoryName": "الخصوبة",
      "categoryDescription": "Fertility related education",
      "arCategoryDescription": "تعليم متعلق بالخصوبة",
      "iconImageUrl": "https://domain.com/images/patient-education/categories/fertility.png",
      "displayOrder": 0,
      "isGeneral": false,
      "educationCount": 15
    },
    {
      "categoryId": 2,
      "categoryName": "General Health",
      "arCategoryName": "الصحة العامة",
      "categoryDescription": "General health tips",
      "arCategoryDescription": "نصائح صحية عامة",
      "iconImageUrl": "https://domain.com/images/patient-education/categories/general.png",
      "displayOrder": 1,
      "isGeneral": true,
      "educationCount": 10
    }
  ],
  "count": 2
}
```

---

## 4. Get Education Content List
**Endpoint:** `GET /api/v2/patient-education/content`

**Authentication:** None (Public)

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | No | Filter by category |
| hasText | bool | No | Filter: has text/HTML content |
| hasPdf | bool | No | Filter: has PDF document |
| hasVideo | bool | No | Filter: has video content |

**Example:** `GET /api/v2/patient-education/content?categoryId=1`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "educationId": 1,
      "categoryId": 1,
      "categoryName": "Fertility",
      "arCategoryName": "الخصوبة",
      "title": "IVF Process Guide",
      "arTitle": "دليل عملية أطفال الأنابيب",
      "hasText": true,
      "hasPdf": true,
      "hasVideo": true,
      "videoType": "Link",
      "videoSource": "YouTube",
      "thumbnailImageUrl": "https://domain.com/images/patient-education/thumbnails/ivf.jpg",
      "summary": "Complete guide to IVF process",
      "arSummary": "دليل كامل لعملية أطفال الأنابيب",
      "displayOrder": 0,
      "imageCount": 5
    }
  ],
  "count": 1
}
```

---

## 5. Get Education Content Detail
**Endpoint:** `GET /api/v2/patient-education/content/{educationId}`

**Authentication:** None (Public)

**Response:**
```json
{
  "success": true,
  "data": {
    "educationId": 1,
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "title": "IVF Process Guide",
    "arTitle": "دليل عملية أطفال الأنابيب",
    "hasText": true,
    "hasPdf": true,
    "hasVideo": true,
    "htmlContent": "<p>IVF (In Vitro Fertilization) is...</p>",
    "arHtmlContent": "<p>أطفال الأنابيب هو...</p>",
    "pdfFileName": "ivf_guide.pdf",
    "pdfFileUrl": "https://domain.com/files/patient-education/pdfs/ivf_guide.pdf",
    "videoType": "Link",
    "videoUrl": "https://www.youtube.com/watch?v=abc123",
    "videoSource": "YouTube",
    "thumbnailImageUrl": "https://domain.com/images/patient-education/thumbnails/ivf.jpg",
    "summary": "Complete guide to IVF process",
    "arSummary": "دليل كامل لعملية أطفال الأنابيب",
    "images": [
      {
        "imageId": 1,
        "educationId": 1,
        "imageFileName": "step1.jpg",
        "imageUrl": "https://domain.com/images/patient-education/content/step1.jpg",
        "imageCaption": "Step 1: Consultation",
        "arImageCaption": "الخطوة 1: الاستشارة",
        "displayOrder": 0
      }
    ],
    "displayOrder": 0,
    "createdAt": "2026-01-01T10:00:00",
    "updatedAt": "2026-01-15T14:30:00"
  }
}
```

---

# 🔐 PATIENT ASSIGNED EDUCATION (Authenticated)

## 6. Get My Assigned Education
**Endpoint:** `GET /api/v2/patient-education/my-education`

**Authentication:** Required (Patient JWT)

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| includeExpired | bool | No | Include expired assignments (default: false) |

**Description:** Returns education content assigned to the logged-in patient by their doctor/clinic.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "assignmentId": 1,
      "educationId": 5,
      "educationTitle": "Post-IVF Care Guide",
      "arEducationTitle": "دليل الرعاية بعد أطفال الأنابيب",
      "categoryName": "Fertility",
      "notes": "Please read before your next appointment",
      "arNotes": "يرجى القراءة قبل موعدك القادم",
      "assignedAt": "2026-01-15T10:00:00",
      "isViewed": false,
      "viewedAt": null,
      "expiresAt": "2026-02-15T10:00:00",
      "isActive": true
    }
  ],
  "count": 1,
  "unviewedCount": 1
}
```

---

## 7. Get Assigned Education Detail
**Endpoint:** `GET /api/v2/patient-education/my-education/{assignmentId}`

**Authentication:** Required (Patient JWT)

**Description:** Returns full education content for an assigned item.

**Response:**
```json
{
  "success": true,
  "data": {
    "assignmentId": 1,
    "patientId": 123,
    "educationId": 5,
    "educationTitle": "Post-IVF Care Guide",
    "arEducationTitle": "دليل الرعاية بعد أطفال الأنابيب",
    "categoryName": "Fertility",
    "notes": "Please read before your next appointment",
    "arNotes": "يرجى القراءة قبل موعدك القادم",
    "assignedAt": "2026-01-15T10:00:00",
    "isViewed": true,
    "viewedAt": "2026-01-16T09:30:00",
    "expiresAt": "2026-02-15T10:00:00",
    "isActive": true,
    "education": {
      "educationId": 5,
      "title": "Post-IVF Care Guide",
      "arTitle": "دليل الرعاية بعد أطفال الأنابيب",
      "hasText": true,
      "hasPdf": true,
      "hasVideo": false,
      "htmlContent": "<p>After your IVF procedure...</p>",
      "arHtmlContent": "<p>بعد عملية أطفال الأنابيب...</p>",
      "pdfFileUrl": "https://domain.com/files/patient-education/pdfs/post_ivf.pdf",
      "thumbnailImageUrl": "https://domain.com/images/patient-education/thumbnails/post_ivf.jpg",
      "images": []
    }
  }
}
```

---

## 8. Mark Education as Viewed
**Endpoint:** `POST /api/v2/patient-education/my-education/{assignmentId}/viewed`

**Authentication:** Required (Patient JWT)

**Description:** Mark an assigned education as viewed by the patient.

**Response:**
```json
{
  "success": true,
  "assignmentId": 1,
  "message": "Marked as viewed"
}
```

---

# 📱 Mobile App Implementation Notes

## Home Screen
1. **Promotions Slider** - Call `GET /api/v2/promotions/slider`
2. **Education Categories** - Call `GET /api/v2/patient-education/categories`

## Education Listing Screen
1. Show categories as tabs/filters
2. Call `GET /api/v2/patient-education/content?categoryId={id}` when category selected

## Education Detail Screen
1. Call `GET /api/v2/patient-education/content/{id}`
2. Display based on content flags:
   - `hasText`: Show HTML content with images
   - `hasPdf`: Show PDF viewer/download button
   - `hasVideo`: Show video player (YouTube embed or native)

## My Education (Patient Portal)
1. Show badge for `unviewedCount`
2. Call `GET /api/v2/patient-education/my-education`
3. On item tap, call detail endpoint and then mark as viewed

## Language Support
All content has English and Arabic versions:
- `title` / `arTitle`
- `description` / `arDescription`
- `htmlContent` / `arHtmlContent`
- etc.

Use based on user's selected language in app settings.

---

# Error Responses

All endpoints return errors in this format:
```json
{
  "success": false,
  "message": "Error description"
}
```

**HTTP Status Codes:**
| Code | Description |
|------|-------------|
| 200 | Success |
| 401 | Unauthorized (missing/invalid token) |
| 404 | Resource not found |
| 500 | Server error |
