# Patient Education Management UI - CRM/Admin Panel

Create a Patient Education management UI with Quill Delta JSON editor for rich text content with inline images.

---

## 1. Education List Page

### Table Columns:
| Column | Description |
|--------|-------------|
| Title | Education title |
| Category | Category name |
| Has Content | Yes/No badge |
| Has PDF | Yes/No badge |
| Status | Active/Inactive badge |
| Created | Date created |
| Actions | Edit, Delete buttons |

### Filters:
- **Category** - Dropdown (from categories API)
- **Status** - All / Active / Inactive

### Actions:
- **Add New** button - Opens create form
- **Edit** - Opens edit form
- **Delete** - Confirmation dialog then delete

---

## 2. Education Create/Edit Form

### Form Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Category | Dropdown | Yes | From `/api/v1/patient-education/categories/dropdown` |
| Title (English) | Text Input | Yes | English title |
| Title (Arabic) | Text Input | No | Arabic title |
| Summary (English) | Textarea | No | Short description |
| Summary (Arabic) | Textarea | No | Arabic short description |
| Thumbnail | File Upload | No | jpg, png, webp, gif (max 5MB) |
| PDF Document | File Upload | No | pdf (max 20MB) with remove button |
| Content (English) | Quill Editor | No | Rich text with inline images |
| Content (Arabic) | Quill Editor | No | Arabic rich text |
| Display Order | Number | No | Sort order |
| Active | Toggle | Yes | Default: true |

---

## 3. Quill Editor Setup

### Install Quill.js:
```bash
npm install quill
# or
yarn add quill
```

### Import in your component:
```javascript
import Quill from 'quill';
import 'quill/dist/quill.snow.css';
```

### Toolbar Configuration:
```javascript
const toolbarOptions = [
  [{ 'header': [1, 2, 3, false] }],
  ['bold', 'italic', 'underline', 'strike'],
  [{ 'color': [] }, { 'background': [] }],
  [{ 'list': 'ordered'}, { 'list': 'bullet' }],
  [{ 'align': [] }],
  ['link', 'image'],
  ['clean']
];

const quill = new Quill('#editor', {
  modules: { 
    toolbar: toolbarOptions 
  },
  theme: 'snow',
  placeholder: 'Write content here...'
});
```

### Custom Image Upload Handler:
```javascript
// Override default image handler to upload to server
function imageHandler() {
  const input = document.createElement('input');
  input.setAttribute('type', 'file');
  input.setAttribute('accept', 'image/jpeg,image/png,image/webp,image/gif');
  input.click();
  
  input.onchange = async () => {
    const file = input.files[0];
    
    // Validate file size (5MB max)
    if (file.size > 5 * 1024 * 1024) {
      alert('Image size must be less than 5MB');
      return;
    }
    
    const formData = new FormData();
    formData.append('file', file);
    
    try {
      // Upload to server
      const response = await fetch('/api/v1/patient-education/content-image', {
        method: 'POST',
        headers: {
          'Authorization': 'Bearer ' + getAuthToken()
        },
        body: formData
      });
      
      if (!response.ok) throw new Error('Upload failed');
      
      const result = await response.json();
      
      // Insert image URL into editor at cursor position
      const range = quill.getSelection(true);
      quill.insertEmbed(range.index, 'image', result.imageUrl);
      quill.setSelection(range.index + 1);
      
    } catch (error) {
      console.error('Image upload failed:', error);
      alert('Failed to upload image');
    }
  };
}

// Register the custom image handler
quill.getModule('toolbar').addHandler('image', imageHandler);
```

### Get Delta JSON for Saving:
```javascript
// Get Delta JSON content to send to API
function getContentDeltaJson() {
  const delta = quill.getContents();
  return JSON.stringify(delta);
}
```

### Load Delta JSON for Editing:
```javascript
// Load saved Delta JSON into editor
function setContentDeltaJson(deltaJsonString) {
  if (deltaJsonString) {
    const delta = JSON.parse(deltaJsonString);
    quill.setContents(delta);
  }
}
```

---

## 4. API Endpoints

### Education CRUD:

| Action | Method | Endpoint | Content-Type |
|--------|--------|----------|--------------|
| List All | GET | `/api/v1/patient-education?categoryId=&includeInactive=false` | - |
| Get by ID | GET | `/api/v1/patient-education/{id}` | - |
| Create | POST | `/api/v1/patient-education` | multipart/form-data |
| Update | PUT | `/api/v1/patient-education/{id}` | multipart/form-data |
| Delete | DELETE | `/api/v1/patient-education/{id}` | - |

### File Uploads:

| Action | Method | Endpoint | Content-Type |
|--------|--------|----------|--------------|
| Upload Thumbnail | POST | `/api/v1/patient-education/{id}/thumbnail` | multipart/form-data |
| Upload PDF | POST | `/api/v1/patient-education/{id}/pdf` | multipart/form-data |
| Remove PDF | DELETE | `/api/v1/patient-education/{id}/pdf` | - |
| Upload Content Image | POST | `/api/v1/patient-education/content-image` | multipart/form-data |

### Categories:

| Action | Method | Endpoint |
|--------|--------|----------|
| List All | GET | `/api/v1/patient-education/categories?includeInactive=false` |
| Dropdown | GET | `/api/v1/patient-education/categories/dropdown` |
| Get by ID | GET | `/api/v1/patient-education/categories/{id}` |
| Create | POST | `/api/v1/patient-education/categories` |
| Update | PUT | `/api/v1/patient-education/categories/{id}` |
| Delete | DELETE | `/api/v1/patient-education/categories/{id}` |

---

## 5. Create/Update Request (multipart/form-data)

### Form Data Fields:

```javascript
const formData = new FormData();

// Required
formData.append('categoryId', categoryId);

// Optional text fields
formData.append('title', title);
formData.append('arTitle', arTitle);
formData.append('summary', summary);
formData.append('arSummary', arSummary);
formData.append('displayOrder', displayOrder);
formData.append('active', active);

// Delta JSON content from Quill editor
formData.append('contentDeltaJson', JSON.stringify(quillEnglish.getContents()));
formData.append('arContentDeltaJson', JSON.stringify(quillArabic.getContents()));

// Optional files
if (thumbnailFile) {
  formData.append('thumbnailFile', thumbnailFile);
}
if (pdfFile) {
  formData.append('pdfFile', pdfFile);
}

// To remove existing PDF (on update)
if (removePdf) {
  formData.append('removePdf', 'true');
}
```

### Submit Request:
```javascript
// Create
const response = await fetch('/api/v1/patient-education', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + getAuthToken()
  },
  body: formData
});

// Update
const response = await fetch(`/api/v1/patient-education/${educationId}`, {
  method: 'PUT',
  headers: {
    'Authorization': 'Bearer ' + getAuthToken()
  },
  body: formData
});
```

---

## 6. Response DTOs

### List Item Response:
```json
{
  "educationId": 1,
  "categoryId": 2,
  "categoryName": "General",
  "arCategoryName": "عام",
  "title": "Diabetes Care",
  "arTitle": "رعاية السكري",
  "hasPdf": true,
  "hasContent": true,
  "thumbnailImageName": "https://..../thumbnails/edu_thumb_1.jpg",
  "summary": "Short description",
  "arSummary": "وصف قصير",
  "displayOrder": 1,
  "active": true,
  "createdAt": "2026-01-22T10:00:00"
}
```

### Detail Response:
```json
{
  "educationId": 1,
  "categoryId": 2,
  "categoryName": "General",
  "arCategoryName": "عام",
  "title": "Diabetes Care",
  "arTitle": "رعاية السكري",
  "contentDeltaJson": "{\"ops\":[{\"insert\":\"Hello World\\n\"}]}",
  "arContentDeltaJson": "{\"ops\":[{\"insert\":\"مرحبا بالعالم\\n\"}]}",
  "pdfFileName": "edu_pdf_1_abc123.pdf",
  "pdfFileUrl": "https://..../pdfs/edu_pdf_1_abc123.pdf",
  "thumbnailImageName": "edu_thumb_1.jpg",
  "thumbnailImageUrl": "https://..../thumbnails/edu_thumb_1.jpg",
  "summary": "Short description",
  "arSummary": "وصف قصير",
  "displayOrder": 1,
  "active": true,
  "createdAt": "2026-01-22T10:00:00",
  "updatedAt": "2026-01-22T12:00:00"
}
```

---

## 7. Category Management

### Category Form Fields:

| Field | Type | Required |
|-------|------|----------|
| Category Name (English) | Text | Yes |
| Category Name (Arabic) | Text | No |
| Description (English) | Textarea | No |
| Description (Arabic) | Textarea | No |
| Icon Image | File Upload | No |
| Display Order | Number | No |
| Is General | Checkbox | No |
| Active | Toggle | Yes |

### Category Request (multipart/form-data):
```javascript
const formData = new FormData();
formData.append('categoryName', categoryName);
formData.append('arCategoryName', arCategoryName);
formData.append('categoryDescription', description);
formData.append('arCategoryDescription', arDescription);
formData.append('displayOrder', displayOrder);
formData.append('isGeneral', isGeneral);
formData.append('active', active);

if (iconImageFile) {
  formData.append('iconImageFile', iconImageFile);
}
```

---

## 8. UI Components Structure

```
PatientEducation/
├── EducationList.tsx          # List page with table
├── EducationForm.tsx          # Create/Edit form
├── QuillEditor.tsx            # Reusable Quill component
├── CategoryList.tsx           # Category management
├── CategoryForm.tsx           # Category create/edit
└── components/
    ├── PdfUpload.tsx          # PDF upload with preview/remove
    ├── ThumbnailUpload.tsx    # Thumbnail upload with preview
    └── CategoryDropdown.tsx   # Category selector
```

---

## 9. Permissions Required

- **PatientEducation.Read** - View list and details
- **PatientEducation.Manage** - Create, Update, Delete

---

## 10. Notes

1. **Quill Delta JSON** stores rich text with formatting and inline images
2. **Images in editor** are uploaded to `/content-image` endpoint and URL is embedded in Delta JSON
3. **PDF is separate** - uploaded/downloaded independently, not part of Delta JSON
4. **Thumbnail** is the cover image shown in lists
5. **Arabic fields** are optional but recommended for RTL support
