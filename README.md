# מערכת לניהול פניות ארגונים – Full Stack

מערכת לניהול פניות ובקשות המתקבלות מארגונים ומעסיקים. המערכת מיועדת לעבודה של מספר משתמשים במקביל, על כמות גדולה של פניות: איתור, סינון, צפייה, עדכון סטטוס (בודד ו-Bulk), היסטוריית שינויים ונתונים מסכמים.

## תוכן עניינים

1. [טכנולוגיות וגרסאות](#טכנולוגיות-וגרסאות)
2. [הוראות הרצה](#הוראות-הרצה)
3. [מבנה הפתרון](#מבנה-הפתרון)
4. [חוזה ה-API](#חוזה-ה-api)
5. [בסיס הנתונים והאינדקסים](#בסיס-הנתונים-והאינדקסים)
6. [מניעת טעינת הנתונים לזיכרון](#מניעת-טעינת-הנתונים-לזיכרון)
7. [Concurrency והיסטוריית שינויים](#concurrency-והיסטוריית-שינויים)
8. [עדכון Bulk](#עדכון-bulk)
9. [Cache ו-Invalidation](#cache-ו-invalidation)
10. [ביצועים](#ביצועים)
11. [Angular](#angular)
12. [בדיקות אוטומטיות](#בדיקות-אוטומטיות)
13. [החלטות טכנולוגיות וחלופות](#החלטות-טכנולוגיות-וחלופות)
14. [מגבלה ידועה ושיפור אפשרי](#מגבלה-ידועה-ושיפור-אפשרי)
15. [תוכנית עבודה וחלוקה למשימות](#תוכנית-עבודה-וחלוקה-למשימות)
16. [שימוש בכלי AI](#שימוש-בכלי-ai)

---

## טכנולוגיות וגרסאות

| רכיב | טכנולוגיה | גרסה |
|---|---|---|
| Backend | ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core (SQL Server) | 9.0.10 |
| בסיס נתונים | SQL Server | SQL Server 2022 Developer |
| Frontend | Angular (Standalone Components, Signals) | Angular 22 |
| Node.js | | v26.13.0 |
| בדיקות Backend | xUnit, Microsoft.AspNetCore.Mvc.Testing | 2.9.2, 9.0.10 |
| בדיקות Frontend | Vitest, Angular TestBed | 5 |

תיעוד ה-API זמין דרך Swagger בסביבת Development.

---

## הוראות הרצה

### דרישות מקדימות

- .NET 9 SDK
- SQL Server מקומי עם Windows Authentication. אם השרת שונה, יש לעדכן את `ConnectionStrings:DefaultConnection` ב-`Backend/API/appsettings.json`.
- Node.js ו-npm
- כלי EF Core: `dotnet tool install --global dotnet-ef`
- תעודת HTTPS לפיתוח: `dotnet dev-certs https --trust`

### 1. הקמת בסיס הנתונים

מתיקיית `Backend`:

```bash
dotnet ef database update --project Repositories --startup-project API
```

הפקודה יוצרת את המסד `OrganizationRequestsDB`, את הטבלאות ואת כל האינדקסים.

### 2. הרצת ה-API

```bash
dotnet run --project API --launch-profile https
```

ה-API זמין בכתובת `https://localhost:7100`, ו-Swagger בכתובת `https://localhost:7100/swagger`.

### 3. יצירת נתוני בדיקה

ה-Endpoint זמין רק בסביבת Development:

```
POST https://localhost:7100/api/dev/seed?reset=true
```

יוצר 100,000 פניות (ניתן לשנות עם `count`). `reset=true` מוחק את כל הנתונים הקיימים ומאפס את המונה, כך שאפשר ליצור את הנתונים מחדש בכל עת. הנתונים נוצרים עם Seed קבוע (`Random(42)`), ולכן התוצאה זהה בכל הרצה. אפשר להריץ דרך Swagger.

### 4. הרצת ה-Frontend

```bash
cd Frontend
npm install
npm start
```

הממשק זמין בכתובת `http://localhost:4200`.

### 5. הרצת הבדיקות

**Backend**, מתיקיית `Backend`:

```bash
dotnet test
```

בדיקות ה-Integration רצות מול מסד נפרד, `RequestsTests`, שמוגדר ב-`appsettings.Testing.json`. המסד נמחק ונוצר מחדש בתחילת כל ריצה. כהגנה, `ApiFactory` מסרב לאפס מסד ששמו אינו מכיל `Test`.

**Frontend**, מתיקיית `Frontend`:

```bash
npm test
```

---

## מבנה הפתרון

```
Backend/
├── API/             Controllers, Program.cs, GlobalExceptionHandler, הגדרות
├── Services/        לוגיקה עסקית, Cache (IServices + מימושים)
├── Repositories/    AppDbContext, קונפיגורציית ישויות, RequestRepository, DbSeeder, Migrations
├── Common/          ישויות (Models), DTOs, Mapping, חוקי מעבר סטטוס
└── API.Tests/       בדיקות Validation ו-Integration

Frontend/src/app/
├── core/            כתובת ה-API, טיפול אחיד בשגיאות HTTP
├── models/          טיפוסים, חוקי מעבר סטטוס בצד הלקוח
├── services/        RequestsApiService ו-Stores (רשימה, סיכום, היסטוריה)
└── components/      רשימה, סינון, Pager, סיכום, היסטוריה

docs/
└── plans/           תוכניות ביצוע שמורות (נפתחות ב-SSMS)
```

**שכבות ותלויות:** `API → Services → Repositories → Common`. כל שכבה רושמת את השירותים שלה ב-DI דרך `AddServices` / `AddRepositories`, כך ש-`Program.cs` לא מכיר את פרטי המימוש.

**Lifetimes ב-DI:**

| שירות | Lifetime | סיבה |
|---|---|---|
| `AppDbContext` | Scoped | אינו Thread-safe, מופע לכל בקשה |
| `RequestRepository`, `RequestService`, `DbSeeder` | Scoped | תלויים ב-`DbContext` |
| `SummaryCache` | Singleton | עוטף את `IMemoryCache` (Singleton ו-Thread-safe). אינו מחזיק `DbContext`, אלא מקבל את פונקציית הטעינה בכל קריאה, ולכן אין Captive Dependency |

**טיפול בשגיאות:** `GlobalExceptionHandler` מחזיר ProblemDetails אחיד. שגיאה לא צפויה מחזירה 500 עם הודעה כללית, והפרטים נרשמים בלוג בלבד. בקשה שהלקוח ביטל מחזירה 499 בלי לרשום שגיאה. Validation מתבצע עם Data Annotations ו-`IValidatableObject`, ו-`[ApiController]` מחזיר 400 עם פירוט השדות.

**Logging:** Conflict נרשם כ-Warning, שינוי סטטוס מוצלח כ-Information, ושגיאה לא צפויה כ-Error עם ה-Exception. לא נרשמים תוכן בקשות או ערכי פרמטרים של SQL.

---

## חוזה ה-API

| Method | נתיב | תיאור | קודים |
|---|---|---|---|
| GET | `/api/requests` | שליפה עם סינון, חיפוש, מיון ודפדוף | 200, 400 |
| PATCH | `/api/requests/{id}/status` | עדכון סטטוס | 200, 400, 404, 409 |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס | 200, 404 |
| POST | `/api/requests/bulk-status` | עדכון סטטוס של עד 100 פניות | 200, 400 |
| GET | `/api/requests/summary` | ספירה לפי סטטוס ולפי עדיפות | 200 |
| GET | `/api/requests/assignees` | רשימת מטפלים, לסינון | 200 |
| POST | `/api/dev/seed` | יצירת נתוני בדיקה (Development בלבד) | 200 |

**פרמטרים של `GET /api/requests`:**

| פרמטר | תיאור | Validation |
|---|---|---|
| `page` | מספר עמוד | 1 עד 1,000,000 |
| `pageSize` | גודל עמוד | 1 עד 100, ברירת מחדל 20 |
| `status`, `priority` | סינון מדויק | ערך Enum חוקי |
| `organizationName` | סינון לפי קידומת שם הארגון | עד 200 תווים |
| `assignedTo` | סינון מדויק לפי מטפל | עד 100 תווים |
| `createdFrom`, `createdTo` | טווח תאריכי יצירה. תאריך בלי שעה ב-`createdTo` כולל את כל היום | `createdFrom` אינו אחרי `createdTo` |
| `search` | חיפוש לפי קידומת בכותרת או בשם הארגון | עד 100 תווים |
| `sortBy` | `CreatedAt`, `UpdatedAt`, `Title`, `OrganizationName`, `Priority`, `Status` | ערך Enum חוקי |
| `sortDir` | `Asc` / `Desc` | ברירת מחדל `Desc` |

התשובות מחזירות DTOs ולא ישויות DB. `RowVersion` מוחזר כמחרוזת Base64 בשדה `version`.

---

## בסיס הנתונים והאינדקסים

### בחירת בסיס הנתונים

נבחר **SQL Server**. הנימוק המלא, והחלופה שנשקלה, מפורטים ב[החלטות טכנולוגיות](#החלטה-1-sql-server-ולא-mongodb).

### מודל

**`Requests`:** `Id`, `Title`, `OrganizationName`, `Status` ו-`Priority` (נשמרים כ-`tinyint`), `AssignedTo`, `CreatedAt`, `UpdatedAt`, `RowVersion` (`rowversion`, מתעדכן אוטומטית על ידי SQL Server בכל שינוי בשורה).

**`RequestStatusHistories`:** `Id`, `RequestId` (Foreign Key), `PreviousStatus`, `NewStatus`, `ChangedAt`, `ChangedBy`.

### אינדקסים ולמה

| אינדקס | תרחיש שהוא משרת |
|---|---|
| `IX_Requests_CreatedAt_Id (CreatedAt DESC, Id) INCLUDE (Title, OrganizationName)` | דפדוף ברירת מחדל, וחיפוש טקסטואלי ללא Key Lookup לכל שורה |
| `IX_Requests_Status_CreatedAt (Status, CreatedAt DESC)` | סינון לפי סטטוס עם מיון לפי תאריך, ו-`GROUP BY Status` |
| `IX_Requests_AssignedTo_CreatedAt (AssignedTo, CreatedAt DESC)` | סינון לפי מטפל עם מיון לפי תאריך |
| `IX_Requests_Title` | חיפוש לפי קידומת בכותרת |
| `IX_Requests_OrganizationName` | חיפוש וסינון לפי קידומת בשם הארגון |
| `IX_Requests_Priority` | `GROUP BY Priority` ומיון עולה לפי עדיפות, נוסף בעקבות המדידה |
| `IX_RequestStatusHistories_RequestId_ChangedAt` | שליפת היסטוריית השינויים של פנייה |

### איך האינדקסים תומכים בתרחישים המרכזיים

| תרחיש | תנאי ב-SQL | אינדקס | מה קורה בפועל |
|---|---|---|---|
| ברירת מחדל: כל הפניות, החדשות קודם | `ORDER BY CreatedAt DESC, Id` | `IX_Requests_CreatedAt_Id` | קריאה לפי סדר האינדקס ועצירה אחרי 20 שורות, בלי Sort (179 קריאות) |
| סינון לפי סטטוס | `WHERE Status = @p ORDER BY CreatedAt DESC, Id` | `IX_Requests_Status_CreatedAt` | ה-COUNT מבצע Index Seek (61 קריאות). המפתח `(Status, CreatedAt DESC)` כולל את `Id` אוטומטית, ולכן הסדר תואם את המיון |
| סינון לפי מטפל | `WHERE AssignedTo = @p ORDER BY CreatedAt DESC, Id` | `IX_Requests_AssignedTo_CreatedAt` | אותו עיקרון כמו סטטוס |
| חיפוש טקסטואלי | `WHERE Title LIKE @s OR OrganizationName LIKE @s` | `IX_Requests_Title`, `IX_Requests_OrganizationName`, ו-`INCLUDE` ב-`IX_Requests_CreatedAt_Id` | חיפוש ממוקד: Seek על שני האינדקסים (72 קריאות). חיפוש רחב: סריקה לפי תאריך, והבדיקה מתבצעת בתוך האינדקס (168 קריאות) |
| סינון לפי ארגון | `WHERE OrganizationName LIKE @s` | `IX_Requests_OrganizationName` | Index Seek על טווח |
| טווח תאריכים | `WHERE CreatedAt >= @from AND CreatedAt < @to` | `IX_Requests_CreatedAt_Id` | Seek על טווח |
| סיכום לפי סטטוס ועדיפות | `GROUP BY Status`, `GROUP BY Priority` | `IX_Requests_Status_CreatedAt`, `IX_Requests_Priority` | סריקה של אינדקס צר במקום הטבלה (237 ו-138 קריאות) |
| היסטוריית פנייה | `WHERE RequestId = @id ORDER BY ChangedAt DESC, Id DESC` | `IX_RequestStatusHistories_RequestId_ChangedAt` | Seek, וסריקה הפוכה של האינדקס נותנת בדיוק את סדר המיון, בלי Sort |

**חיפוש לפי קידומת ולא "מכיל":** החיפוש ממומש עם `StartsWith`, ש-EF מתרגם ל-`LIKE 'טקסט%'`. SQL Server הופך אותו לטווח (`>= 'טקסט' AND < 'טקסע'`) ולכן יכול לבצע Seek על האינדקס, כפי שנראה בתוכנית הביצוע. חיפוש "מכיל" (`LIKE '%טקסט%'`) אינו יכול להשתמש באינדקס רגיל ומחייב סריקה של כל הטבלה בכל חיפוש. **החלופה שנשקלה** לחיפוש "מכיל" היא Full-Text Index של SQL Server. לא נבחרה, כי היא מוסיפה רכיב תשתית ושלב הקמה, וחיפוש לפי תחילת הכותרת או שם הארגון מכסה את התרחיש המרכזי. EF מבצע Escape לתווים מיוחדים כמו `%` ו-`_`, והבדיקה `Search_PercentSign_IsTreatedAsLiteralText` מוודאת זאת.

**שילוב מסננים:** כשנשלחים כמה מסננים יחד, SQL Server משתמש באינדקס אחד, לפי המסנן הסלקטיבי ביותר לפי הערכתו, ושאר התנאים נבדקים על השורות שהאינדקס החזיר. בכמות הנתונים הנוכחית זה מספיק.

---

## מניעת טעינת הנתונים לזיכרון

**העיקרון:** כל סינון, חיפוש, מיון ודפדוף מתבצעים בבסיס הנתונים. לשרת מגיעות רק השורות של העמוד המבוקש, ולדפדפן מגיע רק עמוד אחד.

**איך זה ממומש ב-`RequestRepository.GetPagedAsync`:** השאילתה נבנית כ-`IQueryable` ומורכבת שלב אחר שלב: מוסיפים `Where` רק למסננים שנשלחו, ואז מיון, ואז `Skip / Take`. אף שלב לא מבצע את השאילתה. היא נשלחת למסד רק ב-`CountAsync` וב-`ToListAsync`, ו-EF מתרגם את כל השרשרת ל-SQL אחד עם `WHERE`, `ORDER BY` ו-`OFFSET / FETCH`. אין בקוד `ToList` או `AsEnumerable` לפני הסינון, וזה אומת בלוג ה-SQL של EF Core.

`AsNoTracking` מונע מ-EF לשמור עותק של כל שורה לצורך מעקב שינויים, כי בשליפה לקריאה בלבד אין בכך צורך.

כל בקשת רשימה שולחת שתי שאילתות: `COUNT` לסך התוצאות שתואמות לסינון, ושליפת העמוד. המיפוי ל-DTO מתבצע רק על שורות העמוד, לכל היותר 100.

**הגבלת הקלט:** `PageSize` מוגבל ל-100 ו-`Page` למיליון, באמצעות `[Range]`. בקשה מחוץ לטווח מקבלת 400 עוד לפני שמגיעים למסד. ההגבלה על `Page` גם מונעת חריגה של חישוב ה-`OFFSET` מגבולות `int`.

**מיון יציב:** לכל מיון מתווסף `ThenBy(Id)`. בלי זה, שורות עם אותו ערך (למשל אותה עדיפות) יכולות להופיע בסדר שונה בכל בקשה, ושורה עלולה להופיע בשני עמודים או באף אחד.

**שאר הפעולות:** הסיכום מתבצע כ-`GROUP BY` במסד ומחזיר 4 שורות לסטטוס ו-3 לעדיפות, והתוצאה נשמרת ב-Cache. היסטוריית השינויים מסוננת לפי `RequestId` במסד, ומוחזרים רק השדות של ה-DTO באמצעות `Select`. רשימת המטפלים מחושבת עם `DISTINCT` במסד.

**בצד הלקוח:** Angular שולח את כל הפרמטרים ב-Query String ומציג רק את מה שהשרת החזיר. אין סינון, מיון או דפדוף בדפדפן.

**בדיקה אוטומטית:** `Search_WithPaging_ReturnsOnePageButFullTotal` מוודאת שעם `pageSize=1` מוחזרת שורה אחת, וש-`TotalCount` ו-`TotalPages` משקפים את כל התוצאות.

---

## Concurrency והיסטוריית שינויים

### Optimistic Concurrency

כל פנייה מחזיקה עמודת `rowversion`, ש-SQL Server מעדכן אוטומטית בכל שינוי בשורה. הלקוח מקבל את הערך בשדה `version` ושולח אותו חזרה בבקשת העדכון. האכיפה מתבצעת בשרת ובמסד, לא בממשק.

**שתי שכבות הגנה:**

1. **בדיקה מוקדמת:** ה-Repository טוען את הפנייה ומשווה את `RowVersion` שלה לגרסה שהלקוח שלח. אם הן שונות, מישהו עדכן את הפנייה מאז שהלקוח טען אותה, ומוחזר 409 מיד.
2. **בדיקה אטומית במסד:** `RowVersion` מוגדר ב-EF כ-Concurrency Token. ה-`UPDATE` ש-EF שולח כולל `WHERE Id = @id AND RowVersion = @original`. אם עדכון אחר נכנס בין הטעינה לשמירה, ה-`UPDATE` לא משנה אף שורה, EF זורק `DbUpdateConcurrencyException`, ומוחזר 409.

השכבה השנייה היא זו שמבטיחה נכונות: היא סוגרת את החלון שבין הקריאה לכתיבה. כך נמנע Lost Update: בעדכונים מתחרים רק אחד מצליח, והשני מקבל 409 ויכול לטעון את הנתונים המעודכנים ולהחליט מחדש.

### קודי תשובה בעדכון סטטוס

| מצב | קוד |
|---|---|
| עדכון הצליח | 200, עם הפנייה המעודכנת והגרסה החדשה |
| הפנייה אינה קיימת | 404 |
| סטטוס לא חוקי, או גרסה לא תקינה | 400 (Validation) |
| מעבר סטטוס לא מותר | 400, עם פירוט המעברים המותרים מהסטטוס הנוכחי |
| הפנייה עודכנה בינתיים | 409 |

**מעברים מותרים:** `New → InProgress / Waiting`, `InProgress → Waiting / Completed`, `Waiting → InProgress / Completed`. `Completed` הוא סטטוס סופי. הטבלה מוגדרת במקום אחד בשרת (`RequestStatusTransitions`), ובצד הלקוח מוצגים רק המעברים המותרים.

**עדכון עקבי של השדות:** `UpdatedAt` נקבע בשרת (`DateTime.UtcNow`) ולא מתקבל מהלקוח. `RowVersion` מתעדכן על ידי המסד עצמו.

### היסטוריית שינויים (Audit)

כל שינוי סטטוס מוסיף רשומה ל-`RequestStatusHistories` עם `RequestId`, `PreviousStatus`, `NewStatus`, `ChangedAt` ו-`ChangedBy`. הרשומה נוספת **באותו `SaveChanges`** של העדכון, כלומר באותה טרנזקציה: אם העדכון נכשל על Conflict, גם רשומת ההיסטוריה לא נשמרת. הבדיקות `UpdateStatus_ConcurrentUpdates_OnlyOneSucceedsOtherGetsConflict` ו-`UpdateStatus_InvalidTransition_Returns400AndWritesNoHistory` מוודאות זאת.

`GET /api/requests/{id}/history` מחזיר את השינויים מהחדש לישן, ו-404 אם הפנייה אינה קיימת.

---

## עדכון Bulk

`POST /api/requests/bulk-status` מעדכן סטטוס של עד 100 פניות בבקשה אחת, לאותו סטטוס יעד.

### ההחלטה: Partial Success

כל פנייה מעובדת בנפרד, עם אותה לוגיקה ואותה הגנת Concurrency של עדכון בודד, ונשמרת בטרנזקציה משלה. פנייה שנכשלה אינה מבטלת את האחרות.

**הנימוק:** ב-Optimistic Concurrency, Conflict על פנייה אחת הוא מצב רגיל במערכת מרובת משתמשים, ולא תקלה. ב-All-or-Nothing, גרסה ישנה של פנייה אחת מתוך 100 הייתה מבטלת את כל העדכון, והמשתמש היה צריך לטעון הכול מחדש ולנסות שוב, עם סיכוי ל-Conflict נוסף. ב-Partial Success הוא מקבל תשובה מדויקת לכל פנייה, ומטפל רק במה שנכשל.

**החלופה, All-or-Nothing,** מתאימה כשהפניות תלויות זו בזו מבחינה עסקית, למשל אם קבוצת פניות חייבת לעבור יחד. בדרישות אין תלות כזו.

### התנהגות

**Validation של הבקשה כולה** מתבצע לפני עיבוד כלשהו, ובקשה לא תקינה מקבלת 400 בלי שאף פנייה עודכנה: רשימה ריקה, יותר מ-100 פניות, מזהה כפול, מזהה לא חיובי, או גרסה לא תקינה.

**בקשה תקינה** מקבלת תמיד 200, עם תוצאה לכל פנייה:

```json
{
  "total": 3,
  "succeeded": 1,
  "failed": 2,
  "results": [
    { "id": 10, "outcome": "Success", "request": { "...": "הפנייה המעודכנת" } },
    { "id": 11, "outcome": "Conflict", "error": "The request was modified by someone else. Reload and try again." },
    { "id": 99999, "outcome": "NotFound", "error": "Request 99999 was not found." }
  ]
}
```

ערכי `outcome` אפשריים: `Success`, `NotFound`, `Conflict`, `InvalidTransition`.

**ביטול באמצע:** אם הלקוח מבטל את הבקשה, העיבוד נעצר לפני הפנייה הבאה. פניות שכבר עודכנו נשארות מעודכנות, כי כל אחת נשמרה בטרנזקציה משלה, וה-Cache של הסיכום מתנקה ב-`finally` גם במקרה הזה.

---

## Cache ו-Invalidation

**מה נשמר ב-Cache ולמה:** נתון הסיכום (ספירה לפי סטטוס ולפי עדיפות, `GET /api/requests/summary`). הוא נטען בכל כניסה לדף על ידי כל המשתמשים, מבצע שתי שאילתות `GROUP BY` על כל הטבלה, ואינו תלוי בפרמטרים, ולכן מפתח יחיד מספיק. את רשימת הפניות לא שמרנו ב-Cache: יש אינספור שילובי סינון, מיון ודפדוף, שיעור ה-Hit היה נמוך, והנתון משתנה בתדירות גבוהה.

**Expiration:** 60 שניות, מוחלט (Absolute), כרשת ביטחון. בחרנו מוחלט ולא Sliding, כי תחת עומס קבוע Sliding לא פוקע לעולם.

**Invalidation:** כל עדכון סטטוס שהצליח, בודד או Bulk, מנקה את המפתח, כך שהסיכום הבא נטען מהמסד. ב-Bulk הניקוי נמצא ב-`finally`, כדי שגם בקשה שהופסקה באמצע, אחרי הצלחות חלקיות, לא תשאיר מידע ישן. כדי למנוע מצב שבו שאילתה שהתחילה לפני העדכון שומרת ב-Cache ערך ישן אחרי הניקוי, ה-Cache מחזיק מונה דורות: ערך נשמר רק אם לא בוצע Invalidate בזמן חישובו. בצד הלקוח, אחרי עדכון מוצלח הסיכום נטען מחדש.

**Lifetime:** `SummaryCache` רשום כ-Singleton מעל `IMemoryCache` (Thread-safe). הוא אינו מחזיק `DbContext`, אלא מקבל את פונקציית הטעינה מה-Service ה-Scoped בכל קריאה.

**מספר מופעי שרת:** `IMemoryCache` מקומי לכל מופע. עדכון שבוצע במופע A לא מנקה את ה-Cache של מופע B, ושם המידע עלול להיות ישן עד 60 שניות. בסביבה מבוזרת נחליף את המימוש ל-Cache מבוזר (`IDistributedCache` עם Redis) עם מפתח משותף, או נשלח הודעת ביטול (Pub/Sub) לכל המופעים. ההחלפה פשוטה, כי כל השימוש עובר דרך הממשק `ISummaryCache`. לא הוספנו Redis בפתרון הנוכחי כדי להימנע מ-Over-Engineering.

**מגבלה:** אחרי פקיעה, כמה בקשות במקביל עלולות להריץ את השאילתה כולן (Cache Stampede). אפשר לפתור זאת עם `SemaphoreSlim` סביב היצירה. לא מימשנו, כי השאילתה זולה יחסית (מאות Logical Reads).

---

## ביצועים

### שיטת מדידה

המדידות בוצעו על 100,000 רשומות, ב-SSMS, עם `SET STATISTICS IO ON` ו-`SET STATISTICS PROFILE ON` (תוכנית ביצוע בפועל, כולל מספר השורות שעברו בכל אופרטור).

**המדד המרכזי הוא Logical Reads** (מספר דפי 8KB שנקראו). זמני ריצה תלויים במצב ה-Cache של SQL Server ולכן אינם יציבים. לדוגמה, ה-COUNT הראשון אחרי הפעלת השרת נמדד בלוג ב-433ms, בגלל פתיחת חיבור, הידור תוכנית וקריאה מהדיסק, ובהרצות הבאות ירד משמעותית.

**השאילתות נלקחו מלוג ה-SQL של EF Core** והורצו עם `sp_executesql` ועם אותם פרמטרים. זה חשוב, כי SQL Server בונה את תוכנית הביצוע לפי הפרמטרים, ותוכנית לשאילתה עם ערך קבוע עלולה להיות שונה. ההבדל הזה אכן התגלה במדידות, כפי שמתואר בהמשך.

**תוכניות ביצוע שמורות** בתיקייה [`docs/plans`](docs/plans), ונפתחות ב-SSMS: `01-baseline-before-priority-index.sqlplan` (המדידה הראשונה, לפני אינדקס Priority) ו-`02-ef-status-filter-query.sqlplan` (שאילתת סינון הסטטוס כפי ש-EF שולח אותה).

### פעולה 1: `GET /api/requests` (סינון, חיפוש, מיון ודפדוף)

כל בקשת רשימה שולחת למסד שתי שאילתות: `COUNT` לסך התוצאות, ושליפת העמוד עם `OFFSET / FETCH`. שתיהן מתבצעות במסד, ורק 20 השורות של העמוד מגיעות לשרת.

| תרחיש | Logical Reads | תוכנית |
|---|---|---|
| סינון לפי Status, COUNT | 61 | Index Seek על `IX_Requests_Status_CreatedAt` |
| סינון לפי Status, עמוד ראשון | 182 | Index Seek על `IX_Requests_Status_CreatedAt`, Key Lookup ל-20 שורות בלבד, בלי Sort |
| ללא סינון, עמוד ראשון (ברירת מחדל) | 179 | סריקה מסודרת של `IX_Requests_CreatedAt_Id` |
| חיפוש רחב (`אלפא`, כ-10% מהשורות) | 168 | סריקה לפי תאריך בתוך `IX_Requests_CreatedAt_Id`, עצירה אחרי 20 התאמות, Key Lookup ל-20 שורות בלבד |
| חיפוש ממוקד (`אלפא הנדסה 12`, 22 התאמות) | 72 | Index Seek על `IX_Requests_Title` ועל `IX_Requests_OrganizationName`, ואחריהם TopN Sort על 22 שורות |

### פעולה 2: `GET /api/requests/summary` (Aggregations)

| שאילתה | Logical Reads |
|---|---|
| `GROUP BY Status` | 237 |
| `GROUP BY Priority` (אחרי הוספת אינדקס) | 138 |

שתי השאילתות סורקות אינדקס צר ולא את הטבלה. בנוסף, התוצאה נשמרת ב-Cache, כך שהעלות הזו משולמת לכל היותר פעם בדקה או אחרי עדכון, ולא בכל טעינת דף.

### Bottleneck 1: עמודת Priority ללא אינדקס (תוקן)

במדידה הראשונה גם ה-`GROUP BY Priority` וגם המיון לפי עדיפות ביצעו Clustered Index Scan על כל הטבלה. הוספנו את `IX_Requests_Priority`:

| שאילתה | לפני | אחרי |
|---|---|---|
| `GROUP BY Priority` | 1,561 | 138 |
| מיון `Priority ASC` | 1,561 | 157 |
| מיון `Priority DESC` | 1,561 | 1,561 |

**מיון יורד לא השתפר.** האינדקס מכיל בפועל את `(Priority, Id)` בסדר עולה, כי `Id` הוא המפתח המקבץ. המיון שלנו הוא `Priority DESC, Id ASC`. סריקה הפוכה של האינדקס נותנת `Priority DESC, Id DESC`, שאינו תואם, ולכן SQL Server ממיין את כל הטבלה. ראו הצעות שיפור בהמשך.

### Bottleneck 2: Parameter Sniffing בחיפוש הטקסטואלי (תוקן)

**הממצא:** אותה שאילתת חיפוש עלתה 72 קריאות בהרצה אחת ו-409,229 בהרצה אחרת.

**הסיבה:** EF Core שולח את החיפוש כפרמטר. SQL Server בונה תוכנית בפעם הראשונה שהוא רואה את השאילתה, לפי ערך הפרמטר באותה פעם, ומשתמש בה לכל הערכים הבאים. כשהחיפוש הראשון היה רחב, נבנתה תוכנית שמתאימה לו: לעבור על `IX_Requests_CreatedAt_Id` לפי סדר התאריך, ולבצע Key Lookup לכל שורה כדי לבדוק את הכותרת והארגון, עד שנמצאות 20 התאמות. כשאחריו הגיע חיפוש ממוקד, הוא קיבל את אותה תוכנית.

**ההוכחה:** ניקינו את ה-Plan Cache, הרצנו חיפוש רחב ואחריו חיפוש ממוקד, ולבסוף את החיפוש הממוקד עם `OPTION (RECOMPILE)` כקבוצת ביקורת.

| הרצה | לפני התיקון | אחרי התיקון |
|---|---|---|
| חיפוש רחב (רץ ראשון) | 1,120 | 168 |
| חיפוש ממוקד, עם התוכנית של הרחב | 409,229 | 1,246 |
| חיפוש ממוקד, `RECOMPILE` (ביקורת) | 72 | — |

לפני התיקון, בהרצה השנייה נסרקו 85,881 שורות ובוצעו 85,857 Key Lookups. האומדן של SQL Server לסריקה היה זהה בשתי ההרצות הראשונות (200 שורות), מה שמאשר שהתוכנית לא נבנתה מחדש.

**המשמעות:** במערכת אמיתית, החיפוש הראשון אחרי הפעלת השרת קובע את התוכנית לכל המשתמשים.

**התיקון:** הוספנו ל-`IX_Requests_CreatedAt_Id` את `Title` ו-`OrganizationName` כעמודות `INCLUDE`. כך גם כשנבחרת תוכנית הסריקה, בדיקת התנאי מתבצעת בתוך האינדקס, ו-Key Lookup מתבצע רק ל-20 השורות של העמוד. אחרי התיקון, החיפוש הממוקד עם התוכנית של החיפוש הרחב עולה 1,246 קריאות במקום 409,229. זה בערך גודל האינדקס כולו, כלומר המקרה הגרוע הוא סריקה אחת שלו, בלי Key Lookup לכל שורה. גם החיפוש הרחב השתפר, מ-1,120 ל-168.

**למה `INCLUDE` ולא `RECOMPILE`:** `RECOMPILE` נותן תמיד את התוכנית המדויקת, אבל מחייב הידור בכל חיפוש, ו-EF Core לא תומך ברמזים לשאילתה בלי `DbCommandInterceptor`, כלומר קוד תשתית נוסף. `INCLUDE` הוא שינוי של שורה אחת בקונפיגורציה. המחיר שלו הוא אינדקס גדול יותר והוספת פנייה יקרה מעט יותר. עדכון סטטוס, פעולת הכתיבה המרכזית במערכת, משנה רק את `Status`, `UpdatedAt` ו-`RowVersion`, שאינם באינדקס, ולכן אינו מושפע כלל.

### Bottleneck 3: דפדוף עמוק (מתועד, לא תוקן)

| עמוד | Logical Reads |
|---|---|
| ראשון (OFFSET 0) | 179 |
| אחרון (OFFSET 99,980) | 477,809 |

כדי לדלג על 99,980 שורות, SQL Server עובר עליהן באינדקס, ומכיוון שהשאילתה שולפת את כל העמודות, הוא מבצע Key Lookup גם לשורות שמדלגים עליהן. הדילוג מתבצע רק אחרי השליפה.

לא תיקנו, כי משתמשים כמעט אינם מגיעים לדפים כאלה, והפתרונות משנים את ה-API או את מבנה השאילתה. ראו הצעות שיפור.

### הצעות שיפור

**מיון יורד לפי עדיפות** עדיין סורק את כל הטבלה. יש שתי חלופות: אינדקס נוסף `(Priority DESC, Id)`, או לשנות את מיון ה-`Id` המשני כך שיהיה באותו כיוון של המיון הראשי. אז אינדקס אחד משרת את שני הכיוונים באמצעות סריקה הפוכה, אבל השינוי משפיע על כל המיונים ומחייב לבדוק מחדש את שאר האינדקסים.

**דפדוף עמוק:** שתי חלופות. **Deferred Join**: שולפים קודם רק את 20 ה-Id מהאינדקס עם ה-OFFSET, ורק אחר כך את השורות המלאות. זה שינוי קטן ב-Repository בלי שינוי ב-API. **Keyset Pagination**: "20 השורות שאחרי התאריך וה-Id האחרונים". זה זול בכל עומק, אבל מבטל את האפשרות לקפוץ לעמוד מסוים ומשנה את חוזה ה-API.

---

## Angular

**ארכיטקטורה:** הפרדה בין שלוש שכבות.

| שכבה | אחריות |
|---|---|
| `RequestsApiService` | קריאות HTTP בלבד, בלי State |
| Stores (`RequestsStore`, `SummaryStore`, `HistoryStore`) | State, לוגיקת טעינה, טיפול בשגיאות |
| Components | תצוגה בלבד, כולם `OnPush` |

**פעולות בצד השרת:** החיפוש, הסינון, המיון והדפדוף משנים את ה-Query, שהוא Signal ב-`RequestsStore`. כל שינוי שולח בקשה חדשה לשרת.

**Debounce וביטול בקשות:** הקלדה בשדה החיפוש ובשדה הארגון עוברת `debounceTime(300)`, ורק הטקסט האחרון נשלח. ה-Query מומר ל-Observable ועובר `switchMap`, כך שכשה-Query משתנה בזמן שבקשה קודמת עדיין בדרך, הבקשה הקודמת מבוטלת, ותוצאה ישנה לעולם לא דורסת תוצאה חדשה.

**מצבי תצוגה:** Loading (בטעינה ראשונה מוצגת הודעה, ובטעינה חוזרת הטבלה מתעמעמת), Empty (אין תוצאות לחיפוש), ו-Error עם כפתור "נסי שוב". שגיאה אינה נחשבת "ריק".

**עדכון סטטוס ו-409:** בכל שורה מוצגים רק המעברים המותרים מהסטטוס הנוכחי. אחרי עדכון מוצלח השורה מתעדכנת במקום, והסיכום וההיסטוריה (אם פתוחה) נטענים מחדש. ב-409 הרשימה נטענת מחדש עם הגרסה העדכנית, ומוצגת הודעה שהפנייה עודכנה על ידי משתמש אחר.

**Aggregations והיסטוריה:** כרטיסי סיכום עם ספירות ואחוזים, וחלון היסטוריה לכל פנייה.

---

## בדיקות אוטומטיות

| בדיקה | סוג | מה נבדק |
|---|---|---|
| `SearchTests` | Integration (API + SQL Server) | חיפוש לפי קידומת, שילוב עם סינון, דפדוף עם `TotalCount` מלא, `%` כטקסט רגיל, חיפוש ארוך מדי (400), סינון לפי ארגון, עמוד מחוץ לטווח (400) |
| `UpdateStatusTests` | Integration (API + SQL Server) | עדכון תקין כולל `UpdatedAt`, גרסה חדשה ורשומת היסטוריה; **שני עדכונים מקבילים: אחד 200 ואחד 409, ורשומת היסטוריה אחת בלבד**; פנייה לא קיימת (404); מעבר לא מותר (400 בלי היסטוריה) |
| `UpdateStatusRequestTests` | Unit | Validation של גרסה ושל סטטוס לא מוגדר |
| `BulkUpdateRequestTests` | Unit | רשימה ריקה, 101 פניות, בדיוק 100, מזהים כפולים, גרסה לא תקינה |
| `requests.store.spec.ts` | Angular | Debounce שולח בקשה אחת עם הטקסט האחרון; `switchMap` מבטל בקשה קודמת; 409 טוען מחדש ומציג הודעה; שגיאת שרת ממלאת את מצב השגיאה |

בדיקות ה-Integration רצות דרך `WebApplicationFactory` מול SQL Server אמיתי, עם נתונים קבועים שנוצרים בתחילת הריצה.

---

## החלטות טכנולוגיות וחלופות

### החלטה 1: SQL Server ולא MongoDB

**נבחר:** SQL Server עם EF Core.

**נימוק:** שלוש דרישות מרכזיות נתמכות ישירות במנוע. `rowversion` מספק Optimistic Concurrency ברמת המסד, בלי לנהל גרסה ידנית. העדכון ורשומת ההיסטוריה נשמרים באותה טרנזקציה. ו-`GROUP BY` עם אינדקסים מתאימים מספק את ה-Aggregations ביעילות. בנוסף, EF Core מספק מיגרציות, ו-Execution Plans ב-SSMS אפשרו למדוד ולשפר את הביצועים.

**חלופה שנשקלה, MongoDB:** מתאים למבנה הנתונים, שהוא שטוח יחסית, ותומך בעדכון מותנה בגרסה (`updateOne` עם תנאי על שדה הגרסה) וב-Aggregation Pipeline. לא נבחר, כי הגרסה צריכה להיות מנוהלת ידנית, שמירה אטומית של העדכון וההיסטוריה דורשת טרנזקציה מרובת מסמכים או הטמעת ההיסטוריה בתוך מסמך הפנייה, ולא נדרשת גמישות של סכמה.

### החלטה 2: ניהול State עם Signals ו-RxJS

**נבחר:** Store פשוט לכל תחום (רשימה, סיכום, היסטוריה), כ-Service עם Signals. ה-Query הוא Signal, ושליפת הנתונים עוברת RxJS (`switchMap`, `debounceTime`) במקומות שבהם יש צורך בביטול בקשות ובהשהיה.

**נימוק:** Signals מתאימים להחזקת State ולתצוגה עם `OnPush`, ו-RxJS מתאים לזרימה אסינכרונית של בקשות. השילוב נותן את כל מה שהדרישות צריכות, עם מעט קוד ובלי ספרייה נוספת.

**חלופה שנשקלה, NgRx:** מספק Actions, Reducers ו-Effects, ומתאים למערכת גדולה עם State משותף בין מסכים רבים. כאן יש מסך אחד ושלושה תחומי State עצמאיים, ולכן NgRx היה מוסיף מורכבות ניכרת בלי תועלת, בניגוד לדגש של המבדק על הימנעות מ-Over-Engineering.

**החלטות נוספות** מנומקות לאורך המסמך: [Partial Success ב-Bulk](#ההחלטה-partial-success), [`INCLUDE` לעומת `RECOMPILE`](#bottleneck-2-parameter-sniffing-בחיפוש-הטקסטואלי-תוקן), [חיפוש לפי קידומת לעומת Full-Text](#איך-האינדקסים-תומכים-בתרחישים-המרכזיים), ו-[`IMemoryCache` לעומת Redis](#cache-ו-invalidation).

---

## מגבלה ידועה ושיפור אפשרי

### מגבלה ידועה: זהות המשתמש מגיעה מהלקוח

אין במערכת אימות משתמשים. השדה `ChangedBy`, שנשמר בהיסטוריית השינויים, נשלח מהלקוח בגוף הבקשה, ובממשק הוא קבוע (`מטפל 01`). כלומר, כל לקוח יכול לרשום שינוי בשם כל משתמש, והיסטוריית השינויים אינה אמינה מבחינת Audit.

**במערכת אמיתית:** מוסיפים אימות (למשל JWT מול ספק זהויות ארגוני), ו-`ChangedBy` נלקח בשרת מזהות המשתמש המאומת, ולא מגוף הבקשה.

### שיפור אפשרי: Bulk בשאילתה אחת

כרגע עדכון Bulk מעבד כל פנייה בנפרד: טעינה ושמירה לכל אחת, כלומר עד 200 פניות למסד עבור 100 פניות. זה מספיק לגבול של 100, ושומר על לוגיקה אחת משותפת לעדכון בודד ול-Bulk.

**השיפור:** לטעון את כל הפניות בשאילתה אחת (`WHERE Id IN (...)`), לבצע את הבדיקות בזיכרון, ולשמור את כל השינויים המוצלחים ב-`SaveChanges` אחד. כדי לשמור על Partial Success, Conflict על פנייה אחת לא אמור לבטל את האחרות. לכן צריך לטפל ב-`DbUpdateConcurrencyException` ברמת הישות: להסיר את הישויות שנכשלו מה-Change Tracker ולשמור שוב את השאר.

### מגבלות נוספות

מתועדות בסעיפים הרלוונטיים: Cache מקומי לכל מופע שרת ו-Cache Stampede ([Cache](#cache-ו-invalidation)), מיון יורד לפי עדיפות ודפדוף עמוק ([ביצועים](#הצעות-שיפור)), וחיפוש לפי קידומת בלבד ([אינדקסים](#איך-האינדקסים-תומכים-בתרחישים-המרכזיים)).

---

## תוכנית עבודה וחלוקה למשימות

### 1. עקרונות התכנון

- **בסיס קודם, פיצ'רים אחר כך:** מודל נתונים ו-Migrations לפני כל קוד לוגיקה, כי כל השאר נשען עליהם.
- **Backend לפני Frontend:** ה-API והחוזה (DTOs, קודי שגיאה) מגובשים לפני בניית ה-Angular.
- **פרוסות אנכיות:** כל Epic (שליפה, עדכון, Bulk, Cache) נסגר מקצה לקצה כולל בדיקות, ולא נשאר "לסוף".
- **מדידה לפני אופטימיזציה:** אינדקסים נוספו אחרי שהיו נתונים (100,000 רשומות) ו-Query Plan, ולא על סמך ניחוש.
- **פשטות:** שכבות Controller / Service / Repository ללא Over-Engineering; Cache בזיכרון עם הסבר להרחבה ל-Scale-out.

### 2. מבנה הפרויקט

| פרויקט | אחריות |
|---|---|
| Common | Models, DTOs, Enums, Mapper, כללי מעבר סטטוס |
| Repositories | AppDbContext, Configurations, Migrations, DbSeeder, גישה לנתונים |
| Services | לוגיקה עסקית, Cache, DI |
| API | Controllers, Global Exception Handler (ProblemDetails), הגדרות |
| API.Tests | בדיקות Integration ובדיקות Validation |
| Frontend | Angular – Components, Stores (Signals + RxJS), API Service |

### 3. סדר ביצוע ותלויות

E1 תשתית ← E2 נתונים ← E3 שליפה ← E4 עדכון + Audit ← E5 Bulk ← E6 Cache ← E7 Angular ← E8 ביצועים ← E10 תיעוד והגשה. בדיקות (E9) נכתבות לאורך הדרך, בצמוד לכל Epic.

| Epic | תלוי ב- |
|---|---|
| E2 נתונים | E1 |
| E3 שליפה | E2 |
| E4 עדכון + Audit | E2 |
| E5 Bulk | E4 |
| E6 Cache | E3 (Summary), E4 (Invalidation) |
| E7 Angular | חוזה API מיוצב מ-E3–E6 |
| E8 ביצועים | E2, E3 (ואינדקסים), E6 |
| E9 בדיקות | נכתבות לצד E3–E7 |
| E10 תיעוד | כל השאר |

**קשר לדרישות המבדק:**

| Epic | סעיף במבדק |
|---|---|
| E1 תכנון ותשתית | 9, 10 |
| E2 נתונים | מבנה פנייה, 100,000 רשומות, 6 |
| E3 שליפה | 1 |
| E4 עדכון + Audit | 2, 3 |
| E5 Bulk | 4 |
| E6 Cache | 7 |
| E7 Angular | 5 |
| E8 ביצועים | 6 |
| E9 בדיקות | 8 |
| E10 תיעוד והגשה | 11, אופן ההגשה |

### 4. פירוט משימות


#### E1 – תכנון ותשתית

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 1.1 | פירוק האפיון, תוכנית עבודה ובחירת טכנולוגיות | מסמך זה + החלטות (SQL Server, EF Core 9, Angular Signals) | 3 |
| 1.2 | הקמת Solution בשכבות והפניות בין פרויקטים | Common / Repositories / Services / API, net9.0 | 2 |
| 1.3 | הגדרת DI, Swagger, CORS, Logging | Program.cs, DependencyInjection בכל שכבה | 2 |
| 1.4 | Global Exception Handler | ProblemDetails אחיד, טיפול ב-OperationCanceled | 2 |

#### E2 – מודל נתונים ובסיס נתונים

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 2.1 | תכנון מודל: Request ו-RequestStatusHistory | Entities, Enums (Status, Priority), Configurations | 3 |
| 2.2 | RowVersion לזיהוי עדכון מתחרה | שדה rowversion + Concurrency Token | 1 |
| 2.3 | Migration ראשון והחלתו על OrganizationRequestsDB | InitialCreate | 1 |
| 2.4 | Seeder ל-100,000 רשומות עם יצירה חוזרת | DbSeeder (SqlBulkCopy, Random עם Seed קבוע, פרמטר reset) + endpoint פיתוח | 3 |
| 2.5 | אינדקסים לפי תרחישי השימוש | Migrations AddIndexes ו-AddSearchIndexes | 3 |

#### E3 – שליפת נתונים

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 3.1 | חוזה ה-API: RequestQuery, RequestDto, PagedResult | DTOs עם Validation (Page, PageSize עד 100, אורכי טקסט, טווח תאריכים) | 3 |
| 3.2 | סינון משולב ודפדוף בצד שרת | Status, Priority, OrganizationName, AssignedTo, טווח תאריכים; Skip/Take ב-DB | 4 |
| 3.3 | חיפוש טקסטואלי ב-Title וב-OrganizationName | חיפוש Prefix התומך באינדקס, כולל הגנה על תווי LIKE | 2 |
| 3.4 | מיון בשישה שדות Asc/Desc | ApplySorting עם מיון משני לפי Id (דפדוף יציב) | 2 |
| 3.5 | Aggregations | Summary לפי סטטוס ולפי עדיפות (GroupBy ב-DB) | 2 |
| 3.6 | רשימת מטפלים לסינון | Endpoint assignees | 1 |
| 3.7 | CancellationToken לכל פעולות ה-I/O | מועבר לאורך כל השכבות | 1 |

#### E4 – עדכון סטטוס, תחרותיות והיסטוריה

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 4.1 | כללי מעבר סטטוס מותרים | RequestStatusTransitions + הודעת שגיאה ברורה | 2 |
| 4.2 | Optimistic Concurrency | בדיקת Version בקוד + אכיפה ב-DB (rowversion) ותפיסת DbUpdateConcurrencyException | 4 |
| 4.3 | קודי HTTP | 404 / 400 / 409 כ-ProblemDetails | 1 |
| 4.4 | Audit | שמירת היסטוריה באותה Transaction של העדכון (SaveChanges אחד) | 2 |
| 4.5 | Endpoint להיסטוריית פנייה | GET history, כולל 404 לפנייה לא קיימת | 1 |

#### E5 – עדכון Bulk

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 5.1 | החלטת Partial Success וניסוח הנימוק | כל פנייה מטופלת בנפרד; חלק מהפניות יכולות להיכשל מבלי לבטל את האחרות | 1 |
| 5.2 | Validation לבקשה | 1–100 פריטים, ללא כפילויות, Version תקין | 2 |
| 5.3 | מימוש ותשובה ברורה | BulkUpdateResult: Total / Succeeded / Failed ותוצאה לכל פריט (Success / NotFound / Conflict / InvalidTransition) | 3 |

#### E6 – Cache

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 6.1 | Cache לנתון המסכם | SummaryCache (IMemoryCache), Expiration של 60 שניות | 2 |
| 6.2 | Invalidation | ניקוי לאחר עדכון מוצלח (יחיד ו-Bulk) | 1 |
| 6.3 | תיעוד התאמה ל-Scale-out | מעבר ל-Distributed Cache (Redis) ושידור Invalidation – מתועד ב-README | 1 |
| 6.4 | מניעת שמירת ערך ישן ב-Cache | מונה דורות: ערך לא נשמר אם בוצע Invalidate בזמן חישובו | 1 |

#### E7 – Angular

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 7.1 | מודלים, API Service וטיפול בשגיאות | request.models, RequestsApiService, http-error | 3 |
| 7.2 | RequestsStore | Signals + RxJS: switchMap לביטול בקשות, Loading / Error / Empty | 4 |
| 7.3 | רשימה, מיון ודפדוף | Components: requests-list, pager | 4 |
| 7.4 | סינון וחיפוש עם Debounce של 300ms | requests-filters (חיפוש, ארגון, סטטוס, עדיפות, מטפל, תאריכים) | 4 |
| 7.5 | עדכון סטטוס וטיפול ב-409 | רענון הרשימה והודעה למשתמש | 3 |
| 7.6 | הצגת Aggregations | requests-summary + SummaryStore | 2 |
| 7.7 | חלון היסטוריית שינויים | request-history + HistoryStore | 3 |

#### E8 – ביצועים

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 8.1 | בחירת שתי פעולות מרכזיות | שליפת רשימה עם סינון + מיון, ו-Summary | 1 |
| 8.2 | מדידה על 100,000 רשומות | שאילתות מלוג EF, STATISTICS IO ותוכניות ביצוע בפועל, שמורות ב-docs/plans | 4 |
| 8.3 | זיהוי Bottlenecks ותיקון | אינדקס Priority, INCLUDE לחיפוש (Parameter Sniffing), תיעוד דפדוף עמוק, מדידה חוזרת לפני / אחרי | 4 |

#### E9 – בדיקות אוטומטיות

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 9.1 | תשתית Integration | ApiFactory על מסד נתוני בדיקה נפרד, עם הגנה מפני איפוס מסד שאינו Test | 3 |
| 9.2 | בדיקות שליפה, חיפוש וסינון | SearchTests | 3 |
| 9.3 | בדיקות עדכון: תקין, 404, מעבר לא חוקי, Conflict בין שתי בקשות מקבילות | UpdateStatusTests | 4 |
| 9.4 | בדיקות Validation | UpdateStatusRequestTests, BulkUpdateRequestTests | 2 |
| 9.5 | בדיקות Frontend ל-RequestsStore | Debounce, ביטול בקשה, 409, מצב שגיאה | 3 |

#### E10 – תיעוד והגשה

| # | משימה | תוצר | מאמץ |
|---|---|---|---|
| 10.1 | README: הרצה, טכנולוגיות וגרסאות, מבנה הפתרון | הוראות הפעלה ברורות | 3 |
| 10.2 | תיעוד DB ואינדקסים, Concurrency, Bulk, Cache ו-Invalidation | פרקים ב-README | 3 |
| 10.3 | שתי החלטות טכנולוגיות וחלופות, מגבלה אחת ושיפור אחד | פרק החלטות | 2 |
| 10.4 | פירוט שימוש בכלי AI | מה נעשה בעזרת AI ומה נבדק ושונה ידנית | 1 |
| 10.5 | בדיקה על סביבה נקייה | מסד ריק, Migrations, Seed, הרצת כל הבדיקות והממשק לפי ה-README | 2 |
| 10.6 | פרסום ב-Git | Repository נגיש לבודקים, ללא קבצי Build | 1 |

### 5. סיכום מאמץ

| Epic | הערכה (שעות) |
|---|---|
| E1 תכנון ותשתית | 9 |
| E2 נתונים | 11 |
| E3 שליפה | 15 |
| E4 עדכון + Audit | 10 |
| E5 Bulk | 6 |
| E6 Cache | 5 |
| E7 Angular | 23 |
| E8 ביצועים | 9 |
| E9 בדיקות | 15 |
| E10 תיעוד והגשה | 12 |
| **סה״כ** | **115** |



---

## שימוש בכלי AI

**כלי:** Claude (Anthropic).

### שלב התכנון וכתיבת הקוד

**תכנון:** נעזרתי ב-AI לפרק את הדרישות למשימות, לקבוע סדר עדיפויות ולזהות תלויות בין המשימות.

**מימוש:** נעזרתי בו בכתיבת חלק מהפונקציות ובבחינת דרכי מימוש חלופיות. כל קוד שהוצע נבדק מול הדרישות ומול התנהגות המערכת, ושונה לפי הצורך. לא שילבתי קוד שלא הבנתי.

### שלב הבדיקה, הביצועים והתיעוד

השתמשתי ב-AI לסקירת הקוד מול הדרישות, ולתכנון ופענוח של מדידות הביצועים. דוגמאות:

- **סקירת קוד:** ה-AI זיהה מצב תחרות ב-Cache: שאילתה שהתחילה לפני עדכון יכלה לשמור ערך ישן אחרי ה-Invalidation. בדקתי את התרחיש, והטמעתי את התיקון (מונה דורות). כמו כן זוהתה חריגה אפשרית בחישוב ה-`OFFSET` ממספר עמוד גדול, שהחזירה 500 במקום 400. הוספתי הגבלה ובדיקת Integration שמכסה את המקרה.
- **ביצועים:** ה-AI הציע למדוד את השאילתות כפי ש-EF שולח אותן, עם פרמטרים, ולא שאילתות שכתבתי ביד. ההבדל הזה חשף בעיית Parameter Sniffing: אותה שאילתת חיפוש עלתה 72 קריאות בהרצה אחת ו-409,229 באחרת. את כל המדידות הרצתי בעצמי ב-SSMS, ואימתתי את ההסבר בניסוי עם קבוצת ביקורת לפני שבחרתי בתיקון.
- **תיעוד:** ה-AI סייע בניסוח ובארגון. כל טענה בתיעוד נבדקה מול הקוד ומול תוצאות המדידות שלי.

### מה בוצע ואומת באופן עצמאי

הרצת כל הבדיקות האוטומטיות, כל מדידות הביצועים ואימות תוכניות הביצוע, החלת המיגרציות, ובדיקה ידנית של הממשק.
