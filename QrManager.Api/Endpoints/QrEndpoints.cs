using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QrManager.Api.Contracts;
using QrManager.Api.Data;
using QrManager.Api.Domain;
using QrManager.Api.Options;

namespace QrManager.Api.Endpoints;


public static class QrEndpoints
{
 
    public static IEndpointRouteBuilder MapQrManagerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (HttpContext context) => Results.Ok(new { status = "ok", utc = DateTime.Now, traceId = context.TraceIdentifier }));

        var admin = app.MapGroup("/api").AddEndpointFilter<QrManager.Api.Security.AdminApiKeyEndpointFilter>();
        admin.MapGet("/connection", (HttpContext context) => Results.Ok(new { status = "ok", traceId = context.TraceIdentifier, publicQrPath = "/q/{publicKey}" }));
        admin.MapGet("/qr-codes", ListCodesAsync);
        admin.MapGet("/qr-codes/{id:guid}", GetCodeAsync);
        admin.MapPost("/qr-codes", CreateCodeAsync);
        admin.MapPut("/qr-codes/{id:guid}/target", UpdateTargetAsync);
        admin.MapPost("/qr-codes/{id:guid}/status", UpdateStatusAsync);
        admin.MapGet("/analytics/overview", GetOverviewAsync);
        admin.MapGet("/analytics/recent-scans", GetRecentScansAsync);

        app.MapGet("/q/{publicKey}", ScanAndResolveAsync);
        return app;
    }

    private static async Task<IResult> ListCodesAsync(string? query, QrManagerDbContext db, CancellationToken cancellationToken)
    {
        var normalized = query?.Trim();
        var codes = db.QrCodes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            codes = codes.Where(item => item.Title.Contains(normalized) || item.BookName.Contains(normalized) || item.ChapterName.Contains(normalized));
        }

        var rows = await codes
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new
            {
                Code = item,
                ScanCount = item.Scans.Count,
                LastScanAt = item.Scans.Max(scan => (DateTime?)scan.ScannedAtUtc)
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new QrCodeListResponse(rows.Select(row => row.Code.ToResponse(row.ScanCount, row.LastScanAt)).ToList()));
    }

    private static async Task<IResult> GetCodeAsync(Guid id, QrManagerDbContext db, CancellationToken cancellationToken)
    {
        var row = await db.QrCodes.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                Code = item,
                ScanCount = item.Scans.Count,
                LastScanAt = item.Scans.Max(scan => (DateTime?)scan.ScannedAtUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? Results.NotFound() : Results.Ok(row.Code.ToResponse(row.ScanCount, row.LastScanAt));
    }

    private static async Task<IResult> CreateCodeAsync(CreateQrCodeRequest request, QrManagerDbContext db, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var error = ValidateCreate(request);
        if (error is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = new[] { error } });

        var code = new QrCodeEntity
        {
            PublicKey = await CreateUniqueKeyAsync(db, cancellationToken),
            Title = request.Title.Trim(),
            BookName = request.BookName.Trim(),
            ChapterName = request.ChapterName.Trim(),
            ContentType = request.Target.Type,
            ContentTitle = request.Target.Title.Trim(),
            ContentValue = request.Target.Value.Trim(),
            ContentDescription = string.IsNullOrWhiteSpace(request.Target.Description) ? null : request.Target.Description.Trim(),
            CreatedAtUtc = DateTime.Now,
            UpdatedAtUtc = DateTime.Now
        };
        db.QrCodes.Add(code);
        await db.SaveChangesAsync(cancellationToken);
        loggerFactory.CreateLogger("QrManager.Qr").LogInformation("Created QR {QrCodeId} with public key {PublicKey}", code.Id, code.PublicKey);
        return Results.Created($"/api/qr-codes/{code.Id}", code.ToResponse(0, null));
    }

    //private static async Task<IResult> UpdateTargetAsync(Guid id, UpdateQrTargetRequest request, QrManagerDbContext db, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    //{
    //    var error = ValidateTarget(request.Target);
    //    if (error is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["target"] = new[] { error } });

    //    var code = await db.QrCodes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    //    if (code is null) return Results.NotFound();

    //    code.ContentType = request.Target.Type;
    //    code.ContentTitle = request.Target.Title.Trim();
    //    code.ContentValue = request.Target.Value.Trim();
    //    code.ContentDescription = string.IsNullOrWhiteSpace(request.Target.Description) ? null : request.Target.Description.Trim();
    //    code.ContentVersion++;
    //    code.UpdatedAtUtc = DateTime.Now;
    //    await db.SaveChangesAsync(cancellationToken);
    //    loggerFactory.CreateLogger("QrManager.Qr").LogInformation("Updated target for QR {QrCodeId}; content version {ContentVersion}", code.Id, code.ContentVersion);
    //    var count = await db.ScanEvents.CountAsync(item => item.QrCodeId == code.Id, cancellationToken);
    //    var last = await db.ScanEvents.Where(item => item.QrCodeId == code.Id).MaxAsync(item => (DateTime?)item.ScannedAtUtc, cancellationToken);
    //    return Results.Ok(code.ToResponse(count, last));
    //}

    private static async Task<IResult> UpdateTargetAsync(
    Guid id,
    UpdateQrTargetRequest request,
    QrManagerDbContext db,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
    {
        // Validate request
        if (request?.Target is null)
        {
            return Results.BadRequest(new
            {
                message = "Target information is required."
            });
        }

        var error = ValidateTarget(request.Target);

        if (error is not null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["target"] = new[] { error }
                });
        }

        var code = await db.QrCodes
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (code is null)
            return Results.NotFound();

        code.ContentType = request.Target.Type;
        code.ContentTitle = request.Target.Title.Trim();
        code.ContentValue = request.Target.Value.Trim();
        code.ContentDescription = string.IsNullOrWhiteSpace(request.Target.Description)
            ? null
            : request.Target.Description.Trim();

        code.ContentVersion++;
        code.UpdatedAtUtc = DateTime.Now;

        await db.SaveChangesAsync(cancellationToken);

        loggerFactory.CreateLogger("QrManager.Qr")
            .LogInformation(
                "Updated target for QR {QrCodeId}; content version {ContentVersion}",
                code.Id,
                code.ContentVersion);

        var count = await db.ScanEvents.CountAsync(
            item => item.QrCodeId == code.Id,
            cancellationToken);

        var last = await db.ScanEvents
            .Where(item => item.QrCodeId == code.Id)
            .MaxAsync(item => (DateTime?)item.ScannedAtUtc, cancellationToken);

        return Results.Ok(code.ToResponse(count, last));
    }
    private static async Task<IResult> UpdateStatusAsync(Guid id, UpdateStatusRequest request, QrManagerDbContext db, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var code = await db.QrCodes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (code is null) return Results.NotFound();
        code.IsActive = request.IsActive;
        code.UpdatedAtUtc = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
        loggerFactory.CreateLogger("QrManager.Qr").LogInformation("Changed active state for QR {QrCodeId} to {IsActive}", code.Id, code.IsActive);
        var count = await db.ScanEvents.CountAsync(item => item.QrCodeId == code.Id, cancellationToken);
        var last = await db.ScanEvents.Where(item => item.QrCodeId == code.Id).MaxAsync(item => (DateTime?)item.ScannedAtUtc, cancellationToken);
        return Results.Ok(code.ToResponse(count, last));
    }

    private static async Task<IResult> GetOverviewAsync(QrManagerDbContext db, CancellationToken cancellationToken)
    {
        var today = DateTime.Now.Date;
        var firstDay = today.AddDays(-6);
        var scanRows = await db.ScanEvents.AsNoTracking()
            .Where(item => item.ScannedAtUtc >= firstDay)
            .GroupBy(item => item.ScannedAtUtc.Date)
            .Select(group => new { Day = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var scanMap = scanRows.ToDictionary(item => item.Day, item => item.Count);
        var days = Enumerable.Range(0, 7)
            .Select(offset => firstDay.AddDays(offset))
            .Select(day => new DailyScanResponse(day, scanMap.GetValueOrDefault(day, 0)))
            .ToList();
        var total = await db.ScanEvents.CountAsync(cancellationToken);
        var currentDay = await db.ScanEvents.CountAsync(item => item.ScannedAtUtc >= today, cancellationToken);
        var activeCodes = await db.QrCodes.CountAsync(item => item.IsActive, cancellationToken);
        return Results.Ok(new AnalyticsOverviewResponse(total, currentDay, activeCodes, days));
    }

    private static async Task<IResult> GetRecentScansAsync(int? take, QrManagerDbContext db, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take ?? 30, 1, 100);
        var scans = await db.ScanEvents.AsNoTracking().OrderByDescending(item => item.ScannedAtUtc).Take(limit)
            .Select(item => new ScanEventResponse(item.Id, item.QrCode.Title, item.ScannedAtUtc, item.DeviceType, item.CountryCode))
            .ToListAsync(cancellationToken);
        return Results.Ok(new ScanListResponse(scans));
    }

    private static async Task<IResult> ScanAndResolveAsync(
        string publicKey,
        HttpContext context,
        QrManagerDbContext db,
        IOptions<SecurityOptions> security,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var code = await db.QrCodes.SingleOrDefaultAsync(item => item.PublicKey == publicKey, cancellationToken);
        if (code is null) return Results.NotFound("الرمز غير موجود.");
        if (!code.IsActive) return Results.Content(DisabledPage(code.Title), "text/html; charset=utf-8", statusCode: StatusCodes.Status410Gone);

        db.ScanEvents.Add(new ScanEventEntity
        {
            QrCodeId = code.Id,
            ScannedAtUtc = DateTime.Now,
            DeviceType = GetDeviceType(context.Request.Headers.UserAgent.ToString()),
            CountryCode = context.Request.Headers["CF-IPCountry"].FirstOrDefault() ?? context.Request.Headers["X-Country-Code"].FirstOrDefault(),
            IpHash = HashIp(context.Connection.RemoteIpAddress?.ToString(), security.Value.IpHashSalt),
            UserAgent = Trim(context.Request.Headers.UserAgent.ToString(), 512)
        });
        await db.SaveChangesAsync(cancellationToken);
        loggerFactory.CreateLogger("QrManager.Qr").LogInformation("Recorded public scan for key {PublicKey} as {ContentType}", publicKey, code.ContentType);

        return code.ContentType == ContentType.Text
            ? Results.Content(ContentPage(code), "text/html; charset=utf-8")
            : Results.Redirect(code.ContentValue, permanent: false, preserveMethod: false);
    }

    private static string? ValidateCreate(CreateQrCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 255) return "عنوان الدرس مطلوب ولا يتجاوز 255 حرفًا.";
        if (string.IsNullOrWhiteSpace(request.BookName) || request.BookName.Length > 255) return "اسم الكتاب مطلوب.";
        if (string.IsNullOrWhiteSpace(request.ChapterName) || request.ChapterName.Length > 255) return "اسم الفصل مطلوب.";
        return ValidateTarget(request.Target);
    }

    //private static string? ValidateTarget(ContentTargetRequest target)
    //{
    //    if (string.IsNullOrWhiteSpace(target.Title) || target.Title.Length > 255) return "عنوان المحتوى مطلوب.";
    //    if (string.IsNullOrWhiteSpace(target.Value)) return "المحتوى أو الرابط مطلوب.";
    //    if (target.Type != ContentType.Text && (!Uri.TryCreate(target.Value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))) return "يجب أن يكون الرابط بصيغة HTTP أو HTTPS.";
    //    return null;
    //}


    private static string? ValidateTarget(ContentTargetRequest? target)
    {
        if (target is null)
            return "Target information is required.";

        if (string.IsNullOrWhiteSpace(target.Title) || target.Title.Length > 255)
            return "Content title is required.";

        if (string.IsNullOrWhiteSpace(target.Value))
            return "Content or URL is required.";

        if (target.Type != ContentType.Text &&
            (!Uri.TryCreate(target.Value.Trim(), UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttps &&
              uri.Scheme != Uri.UriSchemeHttp)))
        {
            return "The URL must be HTTP or HTTPS.";
        }

        return null;
    }
    private static async Task<string> CreateUniqueKeyAsync(QrManagerDbContext db, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
            if (!await db.QrCodes.AnyAsync(item => item.PublicKey == key, cancellationToken)) return key;
        }
        throw new InvalidOperationException("تعذر إنشاء مفتاح عام فريد للرمز.");
    }

    private static string GetDeviceType(string userAgent) => userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ? "iPhone" : userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iPad" : "Other";
    private static string? HashIp(string? ip, string salt) => string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(salt) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{ip}"))).ToLowerInvariant();
    private static string? Trim(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];
    private static string ContentPage(QrCodeEntity code) => $"<!doctype html><html lang='ar' dir='rtl'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>{System.Net.WebUtility.HtmlEncode(code.ContentTitle)}</title><style>body{{font-family:Arial,sans-serif;background:#f7f9fc;color:#152033;margin:0;padding:24px;line-height:1.8}}main{{max-width:720px;margin:auto;background:#fff;border:1px solid #e7ecf3;border-radius:18px;padding:24px}}small{{color:#667085}}</style><main><small>{System.Net.WebUtility.HtmlEncode(code.Title)}</small><h1>{System.Net.WebUtility.HtmlEncode(code.ContentTitle)}</h1><p>{System.Net.WebUtility.HtmlEncode(code.ContentValue).Replace("\n", "<br>")}</p></main></html>";
    private static string DisabledPage(string title) => $"<!doctype html><html lang='ar' dir='rtl'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>المحتوى غير متاح</title><style>body{{font-family:Arial,sans-serif;background:#f7f9fc;color:#152033;padding:32px;text-align:center}}div{{max-width:520px;margin:auto;background:#fff;border-radius:18px;padding:28px}}</style><div><h1>المحتوى غير متاح حاليًا</h1><p>تم إيقاف رمز {System.Net.WebUtility.HtmlEncode(title)} مؤقتًا.</p></div></html>";
}
