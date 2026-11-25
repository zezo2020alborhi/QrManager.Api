# عقد API لتطبيق مدير الرموز

كل المسارات الإدارية تستخدم `Content-Type: application/json` ومفتاح الإدارة في الترويسة التالية:

```http
X-Admin-Key: YOUR_ADMIN_API_KEY
```

## إنشاء رمز

```http
POST /api/qr-codes
```

```json
{
  "title": "تجربة الماء والملح",
  "bookName": "علوم الصف الخامس",
  "chapterName": "الفصل الأول",
  "target": {
    "type": "video",
    "title": "فيديو التجربة",
    "value": "https://drive.google.com/file/d/FILE_ID/view",
    "description": "شرح مصور قصير"
  }
}
```

تعيد الاستجابة `publicKey`. يجب أن يُحوّل التطبيق هذا المفتاح إلى رابط ثابت مثل `https://qr.yourdomain.com/q/{publicKey}`، ثم يرسم QR لهذا الرابط فقط.

## تغيير الوجهة من دون تغيير QR

```http
PUT /api/qr-codes/{id}/target
```

أرسل عنصر `target` فقط. يزيد الخادم رقم إصدار المحتوى ويحتفظ بـ `publicKey` نفسه. لا تحذف الرمز ولا تنشئ رمزًا جديدًا عند تغيير الفيديو أو الملف.

## حالة الرمز والإحصاءات

```http
POST /api/qr-codes/{id}/status
{
  "isActive": false
}

GET /api/analytics/overview
GET /api/analytics/recent-scans?take=20
```

عند توقف الرمز يعرض مسار المسح العام صفحة عربية تفيد بأن المحتوى غير متاح. لا يعيد التوجيه إلى الرابط القديم.

## مسار المسح العام

```http
GET /q/{publicKey}
```

يضيف الخادم سجلًا في `ScanEvents` ثم يتعامل مع المحتوى كما يلي:

| نوع المحتوى | الاستجابة |
|---|---|
| `Text` | صفحة HTML عربية من الخادم تحتوي النص الحالي |
| `Link`, `Pdf`, `Image`, `Video` | HTTP Redirect مؤقت إلى رابط المحتوى الحالي |

يُخزّن `IpHash` فقط عند وجود `Security__IpHashSalt`، ولا يخزن IP الخام. احذف هذا الحقل من الخادم والمخطط إن كان التحليل على مستوى الرمز كافيًا لك.

## تكامل Google Drive

ابدأ من API وليس من التطبيق مباشرة:

```http
GET /api/integrations/google-drive/authorization-url
```

يفتح التطبيق قيمة `url` الناتجة في متصفح النظام. يعيد Google المستخدم إلى callback الموجود في إعدادات الخادم، حيث يحفظ الخادم رموز OAuth المحمية. بعد الربط، يمكن استخدام:

```http
GET /api/integrations/google-drive/files
```

لا ترسل Client Secret أو Refresh Token إلى تطبيق Flutter. يعتمد التدفق على OAuth لتفويض الخادم بالوصول إلى الملفات التي اختارها المستخدم. [1] [2]

### المراجع

[1] [Google OAuth 2.0 for Web Server Applications](https://developers.google.com/identity/protocols/oauth2/web-server)

[2] [Google Drive API — Scope Guidance](https://developers.google.com/workspace/drive/api/guides/api-specific-auth)
