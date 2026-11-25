using QrManager.Api.Contracts;
using QrManager.Api.Domain;
using Xunit;

namespace QrManager.Api.Tests;

public sealed class QrMappingsTests
{
    [Fact]
    public void ToResponse_PreservesPublicKeyAndEditableTarget()
    {
        var entity = new QrCodeEntity
        {
            Id = Guid.Parse("9c62a4c5-15b8-4f9f-b648-44783be59b8a"),
            PublicKey = "lesson-001",
            Title = "نشاط الاختبار",
            BookName = "العلوم",
            ChapterName = "الوحدة الأولى",
            ContentType = ContentType.Video,
            ContentTitle = "فيديو النشاط",
            ContentValue = "https://drive.google.com/example",
            ContentVersion = 3,
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var response = entity.ToResponse(12, new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("lesson-001", response.PublicKey);
        Assert.Equal(ContentType.Video, response.Target.Type);
        Assert.Equal(3, response.Target.Version);
        Assert.Equal(12, response.ScanCount);
    }
}
