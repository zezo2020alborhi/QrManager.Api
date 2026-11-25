# متغيرات النشر المطلوبة

لا تضع القيم في هذا المستودع. أضفها في لوحة أسرار الاستضافة أو في ملف بيئة خاص لا يتم إرساله إلى Git.

| المتغير | الغرض |
|---|---|
| `ConnectionStrings__SqlServer` | سلسلة اتصال SQL Server الإنتاجية |
| `Security__AdminApiKey` | مفتاح قوي يرسل من تطبيق الإدارة في ترويسة `X-Admin-Key` |
| `Security__IpHashSalt` | قيمة عشوائية طويلة لتجزئة IP دون تخزينه كنص صريح |
| `GoogleDrive__ClientId` | معرف OAuth من Google Cloud |
| `GoogleDrive__ClientSecret` | سر OAuth من Google Cloud |
| `GoogleDrive__RedirectUri` | مسار callback المنشور في Google Cloud |
| `GoogleDrive__SuccessRedirectUri` | اختياري؛ صفحة أو مخطط URI يعرض نجاح الربط |

يجب أن تستخدم قيم الإنتاج HTTPS. لا تعتمد على العنوان المحلي أو عنوان IP فقط، لأن رمز QR المطبوع يحتاج عنوانًا عامًا ثابتًا وطويل الأمد.
