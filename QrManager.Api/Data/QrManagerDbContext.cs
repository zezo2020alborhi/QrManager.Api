using Microsoft.EntityFrameworkCore;
using QrManager.Api.Domain;

namespace QrManager.Api.Data;

public sealed class QrManagerDbContext(DbContextOptions<QrManagerDbContext> options) : DbContext(options)
{
    public DbSet<QrCodeEntity> QrCodes => Set<QrCodeEntity>();
    public DbSet<ScanEventEntity> ScanEvents => Set<ScanEventEntity>();
    public DbSet<GoogleDriveConnectionEntity> GoogleDriveConnections => Set<GoogleDriveConnectionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QrCodeEntity>(entity =>
        {
            entity.ToTable("QrCodes");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.PublicKey).IsUnique();
            entity.HasIndex(item => new { item.BookName, item.ChapterName });
            entity.Property(item => item.PublicKey).HasMaxLength(96).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(255).IsRequired();
            entity.Property(item => item.BookName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ChapterName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.ContentTitle).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentValue).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(item => item.ContentDescription).HasColumnType("nvarchar(max)");
            entity.HasMany(item => item.Scans).WithOne(item => item.QrCode).HasForeignKey(item => item.QrCodeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScanEventEntity>(entity =>
        {
            entity.ToTable("ScanEvents");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.QrCodeId, item.ScannedAtUtc });
            entity.Property(item => item.DeviceType).HasMaxLength(32);
            entity.Property(item => item.CountryCode).HasMaxLength(8);
            entity.Property(item => item.IpHash).HasMaxLength(128);
            entity.Property(item => item.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<GoogleDriveConnectionEntity>(entity =>
        {
            entity.ToTable("GoogleDriveConnections");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ConnectionKey).IsUnique();
            entity.Property(item => item.ConnectionKey).HasMaxLength(64).IsRequired();
        });
    }
}
