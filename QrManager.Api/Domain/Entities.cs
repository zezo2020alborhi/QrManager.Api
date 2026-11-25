namespace QrManager.Api.Domain;

public enum ContentType
{
    Link,
    Text,
    Pdf,
    Image,
    Video
}

public sealed class QrCodeEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PublicKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string ContentValue { get; set; } = string.Empty;
    public string? ContentDescription { get; set; }
    public int ContentVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.Now;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.Now;
    public ICollection<ScanEventEntity> Scans { get; set; } = new List<ScanEventEntity>();
}

public sealed class ScanEventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QrCodeId { get; set; }
    public QrCodeEntity QrCode { get; set; } = null!;
    public DateTime ScannedAtUtc { get; set; } = DateTime.Now;
    public string? DeviceType { get; set; }
    public string? CountryCode { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class GoogleDriveConnectionEntity
{
    public int Id { get; set; }
    public string ConnectionKey { get; set; } = "default";
    public string AccessTokenProtected { get; set; } = string.Empty;
    public string RefreshTokenProtected { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string? Email { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.Now;
}
