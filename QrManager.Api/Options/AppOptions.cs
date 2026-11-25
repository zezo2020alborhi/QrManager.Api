namespace QrManager.Api.Options;

public sealed class SecurityOptions
{
    public string AdminApiKey { get; init; } = string.Empty;
    public string IpHashSalt { get; init; } = string.Empty;
}

public sealed class GoogleDriveOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string SuccessRedirectUri { get; init; } = string.Empty;
}

public sealed class DatabaseOptions
{
    public bool AutoCreateOnStartup { get; init; }
}
