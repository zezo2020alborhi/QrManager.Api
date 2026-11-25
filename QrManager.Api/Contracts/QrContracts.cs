using QrManager.Api.Domain;

namespace QrManager.Api.Contracts;

public sealed record ContentTargetRequest(ContentType Type, string Title, string Value, string? Description);
public sealed record CreateQrCodeRequest(string Title, string BookName, string ChapterName, ContentTargetRequest Target);
public sealed record UpdateQrTargetRequest(ContentTargetRequest Target);
public sealed record UpdateStatusRequest(bool IsActive);

public sealed record ContentTargetResponse(ContentType Type, string Title, string Value, string? Description, int Version);
public sealed record QrCodeResponse(
    Guid Id,
    string PublicKey,
    string Title,
    string BookName,
    string ChapterName,
    ContentTargetResponse Target,
    bool IsActive,
    int ScanCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastScanAt);

public sealed record QrCodeListResponse(IReadOnlyList<QrCodeResponse> Items);
public sealed record DailyScanResponse(DateTime Day, int Count);
public sealed record AnalyticsOverviewResponse(int TotalScans, int TodayScans, int ActiveCodes, IReadOnlyList<DailyScanResponse> LastSevenDays);
public sealed record ScanEventResponse(Guid Id, string QrTitle, DateTime ScannedAt, string? DeviceType, string? CountryCode);
public sealed record ScanListResponse(IReadOnlyList<ScanEventResponse> Items);
public sealed record DriveFileResponse(string Id, string Name, string MimeType, string? WebViewLink, string? ThumbnailLink, DateTime? ModifiedTime);

public static class QrMappings
{
    public static QrCodeResponse ToResponse(this QrCodeEntity entity, int scanCount, DateTime? lastScanAt) => new(
        entity.Id,
        entity.PublicKey,
        entity.Title,
        entity.BookName,
        entity.ChapterName,
        new ContentTargetResponse(entity.ContentType, entity.ContentTitle, entity.ContentValue, entity.ContentDescription, entity.ContentVersion),
        entity.IsActive,
        scanCount,
        entity.CreatedAtUtc,
        entity.UpdatedAtUtc,
        lastScanAt);
}
