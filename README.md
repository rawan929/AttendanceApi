# AttendanceApi

نظام الحضور الآلي بالكارت. الخطوات 1 إلى 3 (الدخول، الطلاب والدكاترة والمواد، التسجيل في المواد) اتعملت قبل كده.
الملف ده بيشرح الخطوات من 4 إلى 9 وإزاي تجربيها.

| الخطوة | المتطلبات | الملفات الرئيسية |
|---|---|---|
| 4 | FR-08, FR-09 (الأجهزة والكروت) | `DevicesController`, `CardsController` |
| 5 | FR-14 إلى FR-18 (الجلسات) | `SessionsController`, `SessionService`, `SessionExpiryService` |
| 6 | FR-19 إلى FR-25 (مسح الكارت والكروت المعلقة) | `DeviceApiController`, `ScanService`, `ApiKeyAuthHandler` |
| 7 | FR-27 إلى FR-30 (التسجيل اليدوي) | `SessionsController` (`attendance/manual`) |
| 8 | FR-31, FR-32 (العرض اللحظي) | `AttendanceHub` (SignalR), `GET /api/sessions/{id}/live`, `wwwroot/live-test.html` |
| 9 | FR-33 إلى FR-36 (التقارير وواجهة الطالب) | `ReportsController`, `MeController` |

## قبل التشغيل: migration (مرة واحدة)

الخطوات دي ضافت قيدين في الداتابيز: `ApiKeyHash` فريد، وجلسة مفتوحة واحدة بس لكل جهاز.

```bash
dotnet ef migrations add Step4_DeviceKeyAndOpenSessionIndex
dotnet ef database update
dotnet run
```

(في Visual Studio: `Add-Migration Step4_DeviceKeyAndOpenSessionIndex` ثم `Update-Database` من Package Manager Console.)

## قرارات تصميم مهمة

- **الجلسة بتبدأ لحظة فتحها**: الدكتور بيبعت `durationMinutes` (الافتراضي 90)، والسيرفر بيحسب `StartsAt` و `EndsAt` بتوقيته (UTC).
- **الإغلاق التلقائي (FR-16)**: خدمة في الخلفية بتقفل المنتهي كل 30 ثانية، وكمان أي endpoint بيقفل المنتهي قبل ما يشتغل، فالنتيجة دايمًا صح.
- **نتايج المسح بترجع HTTP 200** وفيها `result`: `recorded | already_recorded | not_enrolled | unknown_card | no_session | card_blocked | student_inactive`. المفتاح الغلط بس بيرجع 401.
- **الكارت المجهول بيتحفظ Pending بس لو فيه جلسة مفتوحة** على الجهاز (FR-18). مسح خارج الجلسة مرفوض ومش بيتخزن.
- **الدكتور بيربط الكارت المعلق بطالب مسجل في إحدى موادّه بس**، والأدمن مالوش قيد.
- **الطالب ممكن يبقى له كارت Active واحد بس.** لو الكارت ضاع: `block` ثم ربط الجديد، أو `replace` في خطوة واحدة.
- **تعديل التسجيل بعد انتهاء الجلسة**: الدكتور عنده 15 دقيقة (`Attendance:EditGraceMinutes` في `appsettings.json`)، بعدها الأدمن بس.
- **الدكتور بيلغي التسجيل اليدوي بس.** تسجيلات الكارت الأدمن بس اللي يلغيها.
- **الأوقات (BR-08)**: كلها UTC في الداتابيز والـ JSON (فيها `Z`)، والواجهة بتعرضها محلي.

## التجربة

كل الأمثلة من `https://localhost:7123/scalar/v1`. في Scalar بتحطي الـ token في Authentication، وبتغيّريه لما تبدّلي بين الأدمن والدكتور والطالب.

### أ. تجهيز البيانات (أدمن)

1. `POST /api/auth/login` بحساب الأدمن (`admin@attendance.local` / `Admin@12345`) وانسخي الـ token.
2. `POST /api/doctors` دكتور (سجلي الـ id، وليكن 1).
3. `POST /api/courses` مادة `CS101` للدكتور ده.
4. `POST /api/students` تلات طلاب (ids: 1, 2, 3).
5. `POST /api/courses/1/students` بـ `{ "studentIds": [1, 2] }` (الطالب 3 **مش** مسجل عمدًا).
6. `POST /api/devices` بـ `{ "name": "قاعة 1", "location": "مبنى A" }`
   - انسخي الـ `apiKey` من الرد دلوقتي. **مش هيظهر تاني**، ولو ضاع استخدمي `POST /api/devices/1/rotate-key`.

### ب. فتح جلسة (دكتور) (FR-14, FR-15)

7. login بحساب الدكتور، وحطي الـ token بتاعه.
8. `POST /api/sessions` بـ `{ "courseId": 1, "deviceId": 1, "durationMinutes": 60 }` -> 201، سجلي الـ id (ليكن 1).
9. نفس الطلب تاني -> **409** `device_has_open_session` (FR-15).

### ج. الجهاز يمسح كروت (FR-19 إلى FR-25)

الجهاز مش بيستخدم Scalar. جربيه بـ curl (`-k` عشان شهادة الـ localhost) وحطي مفتاحك مكان `<KEY>`:

```bash
curl -k -X POST https://localhost:7123/api/device/scan \
  -H "X-Api-Key: <KEY>" -H "Content-Type: application/json" \
  -d '{"uid":"04A32B1C"}'
```

| التجربة | النتيجة المتوقعة |
|---|---|
| مفتاح غلط أو من غير مفتاح | 401 |
| `GET /api/device/ping` بالمفتاح الصح | 200 باسم الجهاز ووقت السيرفر |
| مسح `04A32B1C` (كارت جديد) | `unknown_card` |
| مسحه 3 مرات تاني | `unknown_card` كل مرة، وفي `GET /api/cards/pending` (دكتور أو أدمن) **كارت واحد بس** (FR-25) |
| الدكتور: `POST /api/cards/1/link` بـ `{ "studentId": 1 }` | 200 والكارت بقى Active (FR-26) |
| مسح نفس الكارت | `recorded` باسم الطالب |
| مسحه تاني | `already_recorded` (FR-24) |
| الدكتور يحاول يربط كارت بالطالب 3 (مش في مواده) | 403 `student_not_in_your_courses` |
| أدمن: `POST /api/cards` بـ `{ "uid": "AA:BB:CC:DD", "studentId": 3 }` ثم مسحه | `not_enrolled` (BR-02) |
| أدمن: `POST /api/cards/<id>/block` ثم مسحه | `card_blocked` |

### د. العرض اللحظي (FR-31, FR-32)

10. افتحي `https://localhost:7123/live-test.html`، والصقي token الدكتور ورقم الجلسة، واضغطي اتصال.
11. مرري كارت من curl: الاسم لازم يظهر في الصفحة **من غير refresh**، والعدادين يتغيروا.
12. `GET /api/sessions/1/live` بيرجع نفس اللقطة (الحاضرين + اللي لسه ماحضروش).

### هـ. التسجيل اليدوي (FR-27 إلى FR-30)

13. `POST /api/sessions/1/attendance/manual` بـ `{ "studentId": 2 }` -> 200، وبيظهر لحظيًا بـ `method: Manual` واسم الدكتور.
14. نفس الطلب -> 409 `already_recorded`.
15. `{ "studentId": 3 }` -> 400 `student_not_enrolled` (FR-28).
16. `DELETE /api/sessions/1/attendance/2` -> 204، وبيختفي لحظيًا.
17. `DELETE /api/sessions/1/attendance/1` (تسجيل بالكارت) -> 403 `only_manual_can_be_cancelled`.

### و. التمديد والإغلاق (FR-16, FR-17)

18. `POST /api/sessions/1/extend` بـ `{ "extraMinutes": 15 }` -> `endsAt` اتأخر 15 دقيقة.
19. `POST /api/sessions/1/close` -> 204. بعدها المسح بيرجع `no_session` (FR-18)، والتمديد 409.
20. **الإغلاق التلقائي**: افتحي جلسة جديدة بـ `durationMinutes: 1`، استني دقيقة، `GET /api/sessions/2` لازم تبقى `Closed` من غير ما تقفليها.
21. **تعديل بعد الانتهاء**: بعد ما الجلسة تتقفل بـ 15 دقيقة، الدكتور يحاول يضيف يدوي -> 403 `edit_window_closed_admin_only`، والأدمن يقدر.

### ز. التقارير وواجهة الطالب (FR-33 إلى FR-36)

22. (دكتور أو أدمن) `GET /api/reports/courses/1`: لكل طالب `attended`, `held`, `percentage` وخلية لكل جلسة مقفولة. الطالب المسجل اللي مالوش سجل في جلسة مقفولة `present: false` (غايب) (FR-34, BR-03). النسبة = حضر ÷ الجلسات المقفولة (BR-04).
23. login بحساب طالب:
    - `GET /api/me/attendance`: حضوره ونسبته لكل مادة.
    - `GET /api/me/courses/1/sessions`: سجل جلساته.
    - `GET /api/me/card`: حالة كارته (`Active` / `Blocked` / `None`).
    - `GET /api/me/courses/<مادة مش مسجل فيها>/sessions` -> 404.

### ح. الصلاحيات والأمان

| التجربة | المتوقع |
|---|---|
| دكتور تاني (مش صاحب المادة) `GET /api/sessions/1` أو يفتح جلسة على مادة مش بتاعته | 403 (BR-06) |
| طالب `GET /api/sessions` أو `POST /api/sessions` | 403 |
| أدمن `POST /api/sessions` | 403 (الجلسات للدكتور) |
| مفتاح الجهاز على `GET /api/students` | 401 |
| JWT عادي على `POST /api/device/scan` | 401 |
| `POST /api/devices/1/deactivate` ثم أي مسح | 401 فورًا (BR-09) |
| `POST /api/devices/1/rotate-key` | المفتاح القديم يبطل فورًا |
| دكتور يتصل بالـ hub على جلسة دكتور تاني | `JoinSession` يفشل بـ `not_your_session` |
