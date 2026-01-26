# Patient Education Module - CRM UI Development Guide

## Overview
Build a Patient Education management interface in CRM (Coherent HIS) that allows administrators to:
1. Manage Education Categories (e.g., Fertility, Diabetes, General Health)
2. Create/Edit/Delete Educational Content
3. **IMPORTANT:** A single education can have ALL THREE content types together:
   - **Text/HTML** with multiple images
   - **PDF** document
   - **Video** (File Upload OR External Link)

**Base URL:** `{{API_BASE_URL}}/api/v1/patient-education`

**Authentication:** JWT Bearer Token in Header
```
Authorization: Bearer {{access_token}}
```

---

## UI Structure

### 1. Patient Education Categories Page
A page to manage education categories (like disease types/health topics).

### 2. Patient Education Content Page
A page to manage educational content with filtering by category.
**Note:** Each education item can contain Text+Images, PDF, AND Video all together (not mutually exclusive).

---

## API Endpoints & Payloads

---

## 📁 CATEGORY MANAGEMENT

### 1. GET All Categories
**Endpoint:** `GET /api/v1/patient-education/categories`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| includeInactive | boolean | No | Include inactive categories (default: false) |

**Response:**
```json
[
  {
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "categoryDescription": "Fertility and reproductive health education",
    "arCategoryDescription": "تعليم الخصوبة والصحة الإنجابية",
    "iconImageName": "https://domain.com/images/education/icons/category_icon_1_abc123.png",
    "displayOrder": 1,
    "isGeneral": false,
    "active": true,
    "educationCount": 5
  },
  {
    "categoryId": 2,
    "categoryName": "General",
    "arCategoryName": "عام",
    "categoryDescription": "General health education for all patients",
    "arCategoryDescription": "تعليم صحي عام لجميع المرضى",
    "iconImageName": null,
    "displayOrder": 2,
    "isGeneral": true,
    "active": true,
    "educationCount": 10
  }
]
```

---

### 2. GET Category Dropdown List
**Endpoint:** `GET /api/v1/patient-education/categories/dropdown`

**Response:**
```json
[
  {
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "isGeneral": false
  },
  {
    "categoryId": 2,
    "categoryName": "General",
    "arCategoryName": "عام",
    "isGeneral": true
  }
]
```

---

### 3. GET Category by ID
**Endpoint:** `GET /api/v1/patient-education/categories/{categoryId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | Yes | Category ID |

**Response:**
```json
{
  "categoryId": 1,
  "categoryName": "Fertility",
  "arCategoryName": "الخصوبة",
  "categoryDescription": "Fertility and reproductive health education",
  "arCategoryDescription": "تعليم الخصوبة والصحة الإنجابية",
  "iconImageName": "https://domain.com/images/education/icons/category_icon_1_abc123.png",
  "displayOrder": 1,
  "isGeneral": false,
  "active": true
}
```

---

### 4. CREATE Category
**Endpoint:** `POST /api/v1/patient-education/categories`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CategoryName | string | Yes | Category name (English) |
| ArCategoryName | string | No | Category name (Arabic) |
| CategoryDescription | string | No | Description (English) |
| ArCategoryDescription | string | No | Description (Arabic) |
| DisplayOrder | int | No | Display order (default: 0) |
| IsGeneral | boolean | No | Is general category accessible to all patients (default: false) |
| Active | boolean | No | Is active (default: true) |
| IconImageFile | File | No | Icon image (jpg, jpeg, png, webp, gif - max 5MB) |

**Request Example (form-data):**
```
CategoryName: Fertility
ArCategoryName: الخصوبة
CategoryDescription: Fertility and reproductive health education
ArCategoryDescription: تعليم الخصوبة والصحة الإنجابية
DisplayOrder: 1
IsGeneral: false
Active: true
IconImageFile: [FILE]
```

**Response:**
```json
{
  "categoryId": 1,
  "row": {
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "categoryDescription": "Fertility and reproductive health education",
    "arCategoryDescription": "تعليم الخصوبة والصحة الإنجابية",
    "iconImageName": "https://domain.com/images/education/icons/category_icon_1_abc123.png",
    "displayOrder": 1,
    "isGeneral": false,
    "active": true
  }
}
```

---

### 5. UPDATE Category
**Endpoint:** `PUT /api/v1/patient-education/categories/{categoryId}`

**Content-Type:** `multipart/form-data`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | Yes | Category ID |

**Form Fields:** Same as CREATE

**Response:** Same as CREATE

---

### 6. DELETE Category
**Endpoint:** `DELETE /api/v1/patient-education/categories/{categoryId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | Yes | Category ID |

**Response:**
```json
{
  "categoryId": 1
}
```

---

### 7. Upload Category Icon (Separate Endpoint)
**Endpoint:** `POST /api/v1/patient-education/categories/{categoryId}/icon`

**Content-Type:** `multipart/form-data`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | Yes | Category ID |

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | Icon image (jpg, jpeg, png, webp, gif - max 5MB) |

**Response:**
```json
{
  "categoryId": 1,
  "iconImageName": "category_icon_1_abc123.png",
  "iconImageUrl": "https://domain.com/images/education/icons/category_icon_1_abc123.png"
}
```

---

## 📚 EDUCATION CONTENT MANAGEMENT

### 8. GET All Education Content
**Endpoint:** `GET /api/v1/patient-education`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | No | Filter by category |
| hasText | boolean | No | Filter by has text content |
| hasPdf | boolean | No | Filter by has PDF |
| hasVideo | boolean | No | Filter by has video |
| includeInactive | boolean | No | Include inactive content (default: false) |

**Response:**
```json
[
  {
    "educationId": 1,
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "title": "What is Fertility?",
    "arTitle": "ما هي الخصوبة؟",
    "hasText": true,
    "hasPdf": true,
    "hasVideo": true,
    "videoType": "Link",
    "videoSource": "YouTube",
    "thumbnailImageName": "https://domain.com/images/education/thumbnails/edu_thumb_1_xyz.jpg",
    "summary": "Complete fertility education with text, PDF guide, and video",
    "arSummary": "تعليم كامل عن الخصوبة مع نص ودليل PDF وفيديو",
    "displayOrder": 1,
    "active": true,
    "createdAt": "2026-01-14T10:00:00",
    "imageCount": 3
  }
]
```

---

### 9. GET Education Content by ID
**Endpoint:** `GET /api/v1/patient-education/{educationId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| educationId | int | Yes | Education content ID |

**Response:**
```json
{
  "educationId": 1,
  "categoryId": 1,
  "categoryName": "Fertility",
  "arCategoryName": "الخصوبة",
  "title": "What is Fertility?",
  "arTitle": "ما هي الخصوبة؟",
  "hasText": true,
  "hasPdf": true,
  "hasVideo": true,
  "htmlContent": "<h2>What is Fertility?</h2><p>Fertility is the natural capability to produce offspring...</p>",
  "arHtmlContent": "<h2>ما هي الخصوبة؟</h2><p>الخصوبة هي القدرة الطبيعية على الإنجاب...</p>",
  "pdfFileName": "edu_pdf_1_abc.pdf",
  "pdfFileUrl": "https://domain.com/files/education/pdfs/edu_pdf_1_abc.pdf",
  "videoType": "Link",
  "videoFileName": null,
  "videoFileUrl": null,
  "videoUrl": "https://www.youtube.com/watch?v=XXXXX",
  "videoSource": "YouTube",
  "thumbnailImageName": "edu_thumb_1_xyz.jpg",
  "thumbnailImageUrl": "https://domain.com/images/education/thumbnails/edu_thumb_1_xyz.jpg",
  "summary": "Learn about fertility basics",
  "arSummary": "تعرف على أساسيات الخصوبة",
  "images": [
    {
      "imageId": 1,
      "educationId": 1,
      "imageFileName": "edu_img_1_abc.jpg",
      "imageUrl": "https://domain.com/images/education/content/edu_img_1_abc.jpg",
      "imageCaption": "Fertility diagram",
      "arImageCaption": "رسم بياني للخصوبة",
      "displayOrder": 0
    },
    {
      "imageId": 2,
      "educationId": 1,
      "imageFileName": "edu_img_1_def.jpg",
      "imageUrl": "https://domain.com/images/education/content/edu_img_1_def.jpg",
      "imageCaption": "Treatment options",
      "arImageCaption": "خيارات العلاج",
      "displayOrder": 1
    }
  ],
  "displayOrder": 1,
  "active": true,
  "createdAt": "2026-01-14T10:00:00",
  "updatedAt": null
}
```

---

### 10. CREATE Education Content
**Endpoint:** `POST /api/v1/patient-education`

**Content-Type:** `multipart/form-data`

**IMPORTANT:** A single education can have ALL content types together (Text+Images, PDF, Video)

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CategoryId | int | Yes | Category ID |
| Title | string | Yes | Title (English) |
| ArTitle | string | No | Title (Arabic) |
| HasText | boolean | No | Has text/HTML content |
| HasPdf | boolean | No | Has PDF document |
| HasVideo | boolean | No | Has video content |
| HtmlContent | string | No | HTML content (when HasText=true) |
| ArHtmlContent | string | No | Arabic HTML content |
| VideoType | string | No | "Upload" or "Link" (when HasVideo=true) |
| VideoUrl | string | No | External video URL (when VideoType=Link) |
| VideoSource | string | No | "YouTube", "Facebook", "Twitter", "Instagram", "Other" |
| Summary | string | No | Short summary (English) |
| ArSummary | string | No | Short summary (Arabic) |
| DisplayOrder | int | No | Display order |
| Active | boolean | No | Is active (default: true) |
| ThumbnailFile | File | No | Cover/thumbnail image (max 5MB) |
| PdfFile | File | No | PDF document (max 20MB) |
| VideoFile | File | No | Video file (max 100MB, when VideoType=Upload) |
| ContentImages | File[] | No | Multiple images for text content (max 5MB each) |

---

#### Example: CREATE Complete Education (Text + Images + PDF + Video)
```
CategoryId: 1
Title: What is Fertility?
ArTitle: ما هي الخصوبة؟
HasText: true
HasPdf: true
HasVideo: true
HtmlContent: <h2>What is Fertility?</h2><p>Fertility is the natural capability...</p>
ArHtmlContent: <h2>ما هي الخصوبة؟</h2><p>الخصوبة هي القدرة الطبيعية...</p>
VideoType: Link
VideoUrl: https://www.youtube.com/watch?v=XXXXX
VideoSource: YouTube
Summary: Complete fertility education guide
ArSummary: دليل تعليمي شامل للخصوبة
DisplayOrder: 1
Active: true
ThumbnailFile: [IMAGE FILE - cover image]
PdfFile: [PDF FILE - downloadable guide]
ContentImages: [IMAGE FILE 1, IMAGE FILE 2, IMAGE FILE 3] (multiple images for article)
```

---

#### Example: CREATE Text Only
```
CategoryId: 1
Title: Key Factors Affecting Fertility
ArTitle: العوامل الرئيسية المؤثرة على الخصوبة
HasText: true
HtmlContent: <h2>Key Factors</h2><p>Age, nutrition, stress...</p>
Summary: Understanding factors that affect fertility
DisplayOrder: 2
Active: true
ThumbnailFile: [IMAGE FILE - optional]
ContentImages: [IMAGE FILE 1, IMAGE FILE 2] (images for the article)
```

---

#### Example: CREATE PDF Only
```
CategoryId: 1
Title: Fertility Treatment Guide PDF
ArTitle: دليل علاج الخصوبة
HasPdf: true
Summary: Downloadable PDF guide
DisplayOrder: 3
Active: true
ThumbnailFile: [IMAGE FILE - optional]
PdfFile: [PDF FILE]
```

---

#### Example: CREATE Video Only (External Link)
```
CategoryId: 1
Title: Understanding IVF Process
ArTitle: فهم عملية التلقيح الصناعي
HasVideo: true
VideoType: Link
VideoUrl: https://www.youtube.com/watch?v=XXXXX
VideoSource: YouTube
Summary: Video explaining IVF
DisplayOrder: 4
Active: true
ThumbnailFile: [IMAGE FILE - optional]
```

---

#### Example: CREATE Video Only (File Upload)
```
CategoryId: 1
Title: Clinic Tour Video
ArTitle: جولة في العيادة
HasVideo: true
VideoType: Upload
Summary: Virtual tour of our clinic
DisplayOrder: 5
Active: true
ThumbnailFile: [IMAGE FILE - optional]
VideoFile: [VIDEO FILE - mp4, webm, mov, avi, mkv - max 100MB]
```

---

**Response (for all CREATE):**
```json
{
  "educationId": 1,
  "row": {
    "educationId": 1,
    "categoryId": 1,
    "categoryName": "Fertility",
    "arCategoryName": "الخصوبة",
    "title": "What is Fertility?",
    "arTitle": "ما هي الخصوبة؟",
    "hasText": true,
    "hasPdf": true,
    "hasVideo": true,
    "htmlContent": "<h2>What is Fertility?</h2>...",
    "arHtmlContent": "<h2>ما هي الخصوبة؟</h2>...",
    "pdfFileName": "edu_pdf_1_abc.pdf",
    "pdfFileUrl": "https://domain.com/files/education/pdfs/edu_pdf_1_abc.pdf",
    "videoType": "Link",
    "videoFileName": null,
    "videoFileUrl": null,
    "videoUrl": "https://www.youtube.com/watch?v=XXXXX",
    "videoSource": "YouTube",
    "thumbnailImageName": "edu_thumb_1_xyz.jpg",
    "thumbnailImageUrl": "https://domain.com/images/education/thumbnails/edu_thumb_1_xyz.jpg",
    "summary": "Complete fertility education guide",
    "arSummary": "دليل تعليمي شامل للخصوبة",
    "images": [
      {
        "imageId": 1,
        "imageUrl": "https://domain.com/images/education/content/edu_img_1_abc.jpg"
      }
    ],
    "displayOrder": 1,
    "active": true,
    "createdAt": "2026-01-14T10:00:00",
    "updatedAt": null
  }
}
```

---

### 11. UPDATE Education Content
**Endpoint:** `PUT /api/v1/patient-education/{educationId}`

**Content-Type:** `multipart/form-data`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| educationId | int | Yes | Education content ID |

**Form Fields:** Same as CREATE

**Response:** Same as CREATE

---

### 12. DELETE Education Content
**Endpoint:** `DELETE /api/v1/patient-education/{educationId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| educationId | int | Yes | Education content ID |

**Response:**
```json
{
  "educationId": 1
}
```

---

### 13. Upload Thumbnail (Separate Endpoint)
**Endpoint:** `POST /api/v1/patient-education/{educationId}/thumbnail`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | Image file (jpg, jpeg, png, webp, gif - max 5MB) |

**Response:**
```json
{
  "educationId": 1,
  "thumbnailImageName": "edu_thumb_1_xyz.jpg",
  "thumbnailImageUrl": "https://domain.com/images/education/thumbnails/edu_thumb_1_xyz.jpg"
}
```

---

### 14. Upload PDF (Separate Endpoint)
**Endpoint:** `POST /api/v1/patient-education/{educationId}/pdf`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | PDF file (max 20MB) |

**Response:**
```json
{
  "educationId": 1,
  "pdfFileName": "edu_pdf_1_abc123.pdf",
  "pdfFileUrl": "https://domain.com/files/education/pdfs/edu_pdf_1_abc123.pdf"
}
```

---

### 15. Upload Video File (Separate Endpoint)
**Endpoint:** `POST /api/v1/patient-education/{educationId}/video`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | Video file (mp4, webm, mov, avi, mkv - max 100MB) |

**Response:**
```json
{
  "educationId": 1,
  "videoFileName": "edu_video_1_def456.mp4",
  "videoFileUrl": "https://domain.com/files/education/videos/edu_video_1_def456.mp4"
}
```

---

## 🖼️ IMAGE MANAGEMENT ENDPOINTS

### 16. GET All Images for Education
**Endpoint:** `GET /api/v1/patient-education/{educationId}/images`

**Response:**
```json
[
  {
    "imageId": 1,
    "educationId": 1,
    "imageFileName": "edu_img_1_abc.jpg",
    "imageUrl": "https://domain.com/images/education/content/edu_img_1_abc.jpg",
    "imageCaption": "Fertility diagram",
    "arImageCaption": "رسم بياني للخصوبة",
    "displayOrder": 0
  },
  {
    "imageId": 2,
    "educationId": 1,
    "imageFileName": "edu_img_1_def.jpg",
    "imageUrl": "https://domain.com/images/education/content/edu_img_1_def.jpg",
    "imageCaption": "Treatment process",
    "arImageCaption": "عملية العلاج",
    "displayOrder": 1
  }
]
```

---

### 17. Upload Content Image
**Endpoint:** `POST /api/v1/patient-education/{educationId}/images`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | File | Yes | Image file (jpg, jpeg, png, webp, gif - max 5MB) |
| caption | string | No | Image caption (English) |
| arCaption | string | No | Image caption (Arabic) |
| displayOrder | int | No | Display order (auto-calculated if not provided) |

**Response:**
```json
{
  "imageId": 3,
  "educationId": 1,
  "imageFileName": "edu_img_1_ghi.jpg",
  "imageUrl": "https://domain.com/images/education/content/edu_img_1_ghi.jpg",
  "caption": "New image",
  "arCaption": "صورة جديدة",
  "displayOrder": 2
}
```

---

### 18. Update Image Caption/Order
**Endpoint:** `PUT /api/v1/patient-education/{educationId}/images/{imageId}`

**Content-Type:** `application/json`

**Request Body:**
```json
{
  "imageCaption": "Updated caption",
  "arImageCaption": "تعليق محدث",
  "displayOrder": 1
}
```

**Response:**
```json
{
  "imageId": 1,
  "message": "Image updated successfully"
}
```

---

### 19. Delete Content Image
**Endpoint:** `DELETE /api/v1/patient-education/{educationId}/images/{imageId}`

**Response:**
```json
{
  "imageId": 1,
  "message": "Image deleted successfully"
}
```

---

## 🎨 UI/UX Recommendations

### Categories Page
1. **Data Table** with columns: Icon, Name (EN/AR), Description, Type (General/Specific), Order, Status, Actions
2. **Add Category** button opens a modal/form
3. **Edit/Delete** actions in each row
4. **Toggle Active/Inactive** status switch
5. **Drag & Drop** for reordering (update DisplayOrder)

### Education Content Page
1. **Filter Bar** with:
   - Category dropdown
   - Content Type checkboxes (Has Text, Has PDF, Has Video)
   - Active/Inactive toggle
2. **Data Table** with columns: Thumbnail, Title (EN/AR), Category, Content Icons (📝/📄/🎬), Status, Actions
3. **Add Education** button opens a form with TABS or SECTIONS:

### Education Form UI (Add/Edit)
**Use Tabs or Accordion sections for each content type:**

#### Tab 1: Basic Info
- Title (EN/AR)
- Category dropdown
- Summary (EN/AR)
- Thumbnail image upload
- Display Order
- Active toggle

#### Tab 2: Text Content (Optional - Checkbox to enable)
- **Enable Text Content** checkbox → sets HasText=true
- HTML Editor (like Facility Services) for HtmlContent
- HTML Editor for ArHtmlContent
- **Multiple Image Upload** section:
  - Gallery of uploaded images with caption fields
  - Add Image button
  - Drag & Drop to reorder
  - Delete button for each image

#### Tab 3: PDF Document (Optional - Checkbox to enable)
- **Enable PDF** checkbox → sets HasPdf=true
- PDF file upload/replace
- Show current PDF if exists

#### Tab 4: Video Content (Optional - Checkbox to enable)
- **Enable Video** checkbox → sets HasVideo=true
- VideoType radio: Upload / Link
  - **If Upload:** Video file upload field
  - **If Link:** 
    - Video URL input
    - Video Source dropdown (YouTube, Facebook, Twitter, Instagram, Other)

### Content Type Icons in List
Display icons based on what content is included:
- 📝 (HasText = true)
- 📄 (HasPdf = true)
- 🎬 (HasVideo = true)

Example: An education with all three would show: 📝 📄 🎬

### Video Source Icons
Display appropriate icons based on VideoSource:
- YouTube → YouTube icon
- Facebook → Facebook icon
- Twitter → Twitter/X icon
- Instagram → Instagram icon
- Other → Generic link icon

---

## 📋 Permissions Required
- `PatientEducation.Read` - View categories and content
- `PatientEducation.Manage` - Create/Update/Delete categories and content

---

## 🔗 Video URL Examples
| Source | Example URL |
|--------|-------------|
| YouTube | `https://www.youtube.com/watch?v=VIDEO_ID` |
| Facebook | `https://www.facebook.com/watch/?v=VIDEO_ID` |
| Twitter/X | `https://twitter.com/user/status/TWEET_ID` |
| Instagram | `https://www.instagram.com/p/POST_ID/` |
| Other | Any valid video URL |

---

## File Storage Paths
| Type | Path |
|------|------|
| Category Icons | `/images/education/icons/` |
| Thumbnails | `/images/education/thumbnails/` |
| Content Images | `/images/education/content/` |
| PDFs | `/files/education/pdfs/` |
| Videos | `/files/education/videos/` |

---

## 👤 PATIENT EDUCATION ASSIGNMENT

> **Separate Controller:** `PatientEducationAssignmentController`
> **Base Route:** `/api/v1/patient-education-assignments`

Assign education content to specific patients.

---

### 20. Search Patients (for Assignment UI)
**Endpoint:** `GET /api/v1/patients/search`

Use this endpoint to search and select patients when assigning education.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| MRNo | string | No | Patient MR Number |
| Name | string | No | Patient name (partial match) |
| CellPhone | string | No | Patient phone number |
| PageNumber | int | No | Page number (default: 1) |
| PageSize | int | No | Page size (default: 20, max: 100) |

**Response:**
```json
{
  "patients": [
    {
      "mrNo": "MR123456",
      "personFirstName": "Ahmed",
      "personMiddleName": null,
      "personLastName": "Khan",
      "personSex": "M",
      "patientBirthDate": "1990-05-15",
      "personCellPhone": "+971501234567",
      "personEmail": "ahmed@email.com"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

### 21. GET Assignments by Patient
**Endpoint:** `GET /api/v1/patient-education-assignments/by-patient/{patientId}`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| includeExpired | boolean | No | Include expired assignments (default: false) |

**Response:**
```json
[
  {
    "assignmentId": 1,
    "patientId": 123,
    "educationId": 1,
    "educationTitle": "What is Fertility?",
    "arEducationTitle": "ما هي الخصوبة؟",
    "categoryName": "Fertility",
    "assignedByUserId": 10,
    "assignedAt": "2026-01-16T10:00:00",
    "notes": "Please review before next appointment",
    "arNotes": "يرجى المراجعة قبل الموعد القادم",
    "isViewed": false,
    "viewedAt": null,
    "expiresAt": "2026-02-16T10:00:00",
    "isActive": true
  }
]
```

---

### 22. GET Assignments by Education
**Endpoint:** `GET /api/v1/patient-education-assignments/by-education/{educationId}`

**Response:** Same as above (list of assignments)

---

### 23. GET Assignment by ID
**Endpoint:** `GET /api/v1/patient-education-assignments/{assignmentId}`

**Response:**
```json
{
  "assignmentId": 1,
  "patientId": 123,
  "educationId": 1,
  "educationTitle": "What is Fertility?",
  "arEducationTitle": "ما هي الخصوبة؟",
  "categoryName": "Fertility",
  "assignedByUserId": 10,
  "assignedAt": "2026-01-16T10:00:00",
  "notes": "Please review before next appointment",
  "arNotes": null,
  "isViewed": true,
  "viewedAt": "2026-01-16T12:30:00",
  "expiresAt": null,
  "isActive": true,
  "education": {
    "educationId": 1,
    "title": "What is Fertility?",
    "hasText": true,
    "hasPdf": true,
    "hasVideo": false,
    "htmlContent": "<h2>...</h2>",
    "pdfFileUrl": "https://...",
    "images": [...]
  }
}
```

---

### 24. Assign Education to Patient
**Endpoint:** `POST /api/v1/patient-education-assignments`

**Request Body:**
```json
{
  "patientId": 123,
  "educationId": 1,
  "notes": "Please review before next appointment",
  "arNotes": "يرجى المراجعة قبل الموعد القادم",
  "expiresAt": "2026-02-16T10:00:00"
}
```

**Response:**
```json
{
  "assignmentId": 1,
  "patientId": 123,
  "educationId": 1,
  "message": "Education assigned successfully"
}
```

---

### 25. Bulk Assign Education to Multiple Patients
**Endpoint:** `POST /api/v1/patient-education-assignments/bulk`

**Request Body:**
```json
{
  "patientIds": [123, 456, 789],
  "educationId": 1,
  "notes": "Important information for all fertility patients",
  "arNotes": "معلومات مهمة لجميع مرضى الخصوبة",
  "expiresAt": null
}
```

**Response:**
```json
{
  "assignmentIds": [1, 2, 3],
  "patientCount": 3,
  "educationId": 1,
  "message": "Education assigned to 3 patients successfully"
}
```

---

### 26. Mark Assignment as Viewed
**Endpoint:** `POST /api/v1/patient-education-assignments/{assignmentId}/viewed`

**Response:**
```json
{
  "assignmentId": 1,
  "message": "Marked as viewed"
}
```

---

### 27. Update Assignment
**Endpoint:** `PUT /api/v1/patient-education-assignments/{assignmentId}`

**Request Body:**
```json
{
  "notes": "Updated notes",
  "arNotes": "ملاحظات محدثة",
  "expiresAt": "2026-03-16T10:00:00"
}
```

**Response:**
```json
{
  "assignmentId": 1,
  "message": "Assignment updated successfully"
}
```

---

### 28. Delete Assignment
**Endpoint:** `DELETE /api/v1/patient-education-assignments/{assignmentId}`

**Response:**
```json
{
  "assignmentId": 1,
  "message": "Assignment deleted successfully"
}
```

---

### 29. Remove Assignment by Patient & Education
**Endpoint:** `DELETE /api/v1/patient-education-assignments/patient/{patientId}/education/{educationId}`

**Response:**
```json
{
  "patientId": 123,
  "educationId": 1,
  "message": "Assignment removed successfully"
}
```

---

## 🎯 Assignment UI Recommendations

### Patient Education Assignment Page
1. **Search/Select Patient** - Dropdown or search to select patient
2. **Education List** - Show available education content to assign
3. **Assign Button** - Opens modal with:
   - Notes field (EN/AR)
   - Expiry date picker (optional)
4. **Bulk Assign** - Select multiple patients, assign one education

### Patient Detail View (Education Tab)
1. Show assigned education list
2. Show viewed/unviewed status
3. **Remove** button to unassign
4. **Assign More** button

### Education Detail View (Assigned Patients Tab)
1. Show list of patients assigned to this education
2. Show viewed status for each patient
3. **Assign to More Patients** button
