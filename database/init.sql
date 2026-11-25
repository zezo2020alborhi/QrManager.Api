IF DB_ID(N'QrManager') IS NULL
BEGIN
    CREATE DATABASE QrManager;
END
GO

USE QrManager;
GO

IF OBJECT_ID(N'dbo.QrCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QrCodes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_QrCodes PRIMARY KEY,
        PublicKey NVARCHAR(96) NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        BookName NVARCHAR(255) NOT NULL,
        ChapterName NVARCHAR(255) NOT NULL,
        ContentType NVARCHAR(20) NOT NULL,
        ContentTitle NVARCHAR(255) NOT NULL,
        ContentValue NVARCHAR(MAX) NOT NULL,
        ContentDescription NVARCHAR(MAX) NULL,
        ContentVersion INT NOT NULL CONSTRAINT DF_QrCodes_ContentVersion DEFAULT 1,
        IsActive BIT NOT NULL CONSTRAINT DF_QrCodes_IsActive DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_QrCodes_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_QrCodes_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_QrCodes_PublicKey UNIQUE (PublicKey),
        CONSTRAINT CK_QrCodes_ContentType CHECK (ContentType IN (N'Link', N'Text', N'Pdf', N'Image', N'Video'))
    );
    CREATE INDEX IX_QrCodes_BookName_ChapterName ON dbo.QrCodes(BookName, ChapterName);
END
GO

IF OBJECT_ID(N'dbo.ScanEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanEvents
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScanEvents PRIMARY KEY,
        QrCodeId UNIQUEIDENTIFIER NOT NULL,
        ScannedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ScanEvents_ScannedAtUtc DEFAULT SYSUTCDATETIME(),
        DeviceType NVARCHAR(32) NULL,
        CountryCode NVARCHAR(8) NULL,
        IpHash NVARCHAR(128) NULL,
        UserAgent NVARCHAR(512) NULL,
        CONSTRAINT FK_ScanEvents_QrCodes FOREIGN KEY (QrCodeId) REFERENCES dbo.QrCodes(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ScanEvents_QrCodeId_ScannedAtUtc ON dbo.ScanEvents(QrCodeId, ScannedAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.GoogleDriveConnections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoogleDriveConnections
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GoogleDriveConnections PRIMARY KEY,
        ConnectionKey NVARCHAR(64) NOT NULL,
        AccessTokenProtected NVARCHAR(MAX) NOT NULL,
        RefreshTokenProtected NVARCHAR(MAX) NOT NULL,
        AccessTokenExpiresAtUtc DATETIME2 NOT NULL,
        Email NVARCHAR(320) NULL,
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_GoogleDriveConnections_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_GoogleDriveConnections_ConnectionKey UNIQUE (ConnectionKey)
    );
END
GO
