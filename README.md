# QrManager API

هذا المشروع هو خادم **ASP.NET Core 8** للتطبيق Android. افتح `QrManager.sln` في Visual Studio أو Rider؛ فهو يتضمن مشروع API ومشروع الاختبارات. يحتفظ الخادم بالمفتاح العام الثابت لكل رمز QR، ويسجل المسح ثم يعرض النص أو يعيد التوجيه إلى رابط المحتوى الحالي. لذلك يبقى الرمز المطبوع صالحًا عند تغيير المحتوى.

## المكونات

| العنصر | المسؤولية |
|---|---|
| `QrManager.Api` | ASP.NET Core Minimal API، تحويل المسح، إدارة المحتوى والإحصاءات |
| `database/init.sql` | جداول SQL Server والفهارس والقيود الأساسية |
| `docker-compose.yml` | تشغيل SQL Server وAPI محليًا أو على خادم يدعم Docker |
| Google Drive OAuth | حفظ رموز الوصول والتحديث بصورة محمية على الخادم، ثم قراءة بيانات الملفات التي اختارها الناشر |

## تشغيل محلي باستخدام Docker

أنشئ ملف إعدادات خاص بك خارج المستودع يضع القيم التالية: `SA_PASSWORD` و`ADMIN_API_KEY` و`IP_HASH_SALT` وبيانات Google Drive OAuth. ثم نفّذ الأمر التالي من هذا المجلد:

```bash
docker compose up --build
```

بعد أن تصبح حاوية SQL Server جاهزة، نفّذ سكربت الجداول مرة واحدة:

```bash
docker compose exec -T sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -i /seed/init.sql'
```

يبقى تطبيق API متاحًا على `http://localhost:8080` عند تشغيل Docker. أما عند تشغيل `dotnet run` محليًا في بيئة Development، فيستمع API على `http://0.0.0.0:57510` ليسمح للمحاكي أو الهاتف الموجود على الشبكة المحلية بالوصول إليه. تظهر واجهة Swagger في بيئة Development. قبل الإنتاج، انشر API خلف نطاق HTTPS ثابت، مثل `https://qr.example.com`، واجعل هذا النطاق هو القيمة المخزنة في إعدادات التطبيق Flutter.

> لا تضع ملف `.env` أو Google Client Secret أو Admin API Key داخل مستودع عام أو ملف APK.

## اختبار الاتصال المحلي

اضبط مفتاح الإدارة وسر تجزئة IP في البيئة قبل تشغيل التطبيق، ثم شغل API:

```powershell
$env:Security__AdminApiKey = "ضع_قيمة_عشوائية_طويلة_هنا"
$env:Security__IpHashSalt = "ضع_قيمة_عشوائية_مختلفة_هنا"
dotnet run --project .\QrManager.Api
```

بعد ذلك تحقق من العنوان والمفتاح من دون طباعتهما في السجل:

```powershell
.\scripts\Test-Connection.ps1 -ApiBaseUrl "http://localhost:57510" -AdminApiKey $env:Security__AdminApiKey
```

راجع ملف [CONNECTION-SETUP.md](CONNECTION-SETUP.md) لاختيار العنوان الصحيح لتطبيق Chrome أو محاكي Android أو الهاتف الحقيقي. تحفظ السجلات اليومية في `QrManager.Api/logs/`.

## ربط Google Drive

أنشئ OAuth Client من نوع **Web application** في Google Cloud، وفعل Google Drive API. أضف عنوان callback المنشور حرفيًا إلى Authorized Redirect URIs، مثل:

```text
https://api.yourdomain.com/api/integrations/google-drive/callback
```

ضع Client ID وClient Secret في متغيرات البيئة، ثم أدخل عنوان API ومفتاح الإدارة في تطبيق Flutter، واحفظ الإعدادات وأوقف وضع العرض التجريبي. بعد ذلك اضغط **ربط Google Drive**. يبدأ التطبيق الطلب المعتمد من API، ثم يفتح المتصفح لإتمام موافقة Google. يستخدم الخادم نطاق `drive.file` ليقتصر على الملفات التي يختارها المستخدم أو يشاركها مع التطبيق، بدل طلب الوصول الكامل إلى Drive.

## مسارات API

تحتاج كل المسارات الإدارية إلى الترويسة التالية:

```http
X-Admin-Key: قيمة_مفتاح_الإدارة
```

| المسار | العملية | الغرض |
|---|---|---|
| `/api/qr-codes` | GET | قائمة الرموز مع بحث اختياري `query` |
| `/api/qr-codes` | POST | إنشاء رمز برابط عام ثابت |
| `/api/qr-codes/{id}/target` | PUT | تغيير المحتوى من دون تغيير QR |
| `/api/qr-codes/{id}/status` | POST | تفعيل الرمز أو إيقافه |
| `/api/analytics/overview` | GET | إجماليات ومسحات آخر 7 أيام |
| `/api/analytics/recent-scans` | GET | أحدث المسحات |
| `/api/connection` | GET | اختبار العنوان ومفتاح الإدارة قبل حفظ إعدادات Flutter |
| `/api/integrations/google-drive/authorization-url` | GET | إنشاء رابط تفويض Google Drive |
| `/api/integrations/google-drive/files` | GET | قراءة أحدث الملفات المتاحة للتطبيق |
| `/q/{publicKey}` | GET | عام؛ يسجل المسح ثم يعرض النص أو يعيد التوجيه |

## الخصوصية والأمان

لا يسجل الخادم عنوان IP الخام؛ بل يحسب تجزئة SHA-256 مملحة منه لغرض تقليل التكرار والتحليل الداخلي. يمكن حذف حقل `IpHash` بالكامل إذا لم تكن بحاجة إليه. لا تُخزّن ملفات Google Drive في SQL Server، بل معلومات الربط والبيانات الوصفية فقط.
