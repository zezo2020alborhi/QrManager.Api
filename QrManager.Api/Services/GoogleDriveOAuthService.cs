using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QrManager.Api.Contracts;
using QrManager.Api.Data;
using QrManager.Api.Domain;
using QrManager.Api.Options;

namespace QrManager.Api.Services;

public sealed class GoogleDriveOAuthService(
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<GoogleDriveOptions> options,
    QrManagerDbContext db)
{
    private const string Scope = "https://www.googleapis.com/auth/drive.file";
    private readonly GoogleDriveOptions _options = options.Value;
    private readonly IDataProtector _tokenProtector = dataProtectionProvider.CreateProtector("QrManager.GoogleDrive.Tokens.v1");
    private readonly IDataProtector _stateProtector = dataProtectionProvider.CreateProtector("QrManager.GoogleDrive.State.v1");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Uri CreateAuthorizationUri()
    {
        EnsureConfigured();
        var protectedState = _stateProtector.Protect(JsonSerializer.Serialize(new OAuthState(DateTimeOffset.UtcNow.AddMinutes(10)), JsonOptions));
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = Scope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = protectedState
        };
        return new Uri($"https://accounts.google.com/o/oauth2/v2/auth?{ToQueryString(query)}");
    }

    public async Task CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        ValidateState(state);
        var token = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = _options.RedirectUri,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken)) throw new InvalidOperationException("لم يعد Google رمز تحديث. احذف اتصال التطبيق من إعدادات حساب Google ثم أعد المحاولة.");
        await SaveTokenAsync(token, cancellationToken);
    }

    public async Task<IReadOnlyList<DriveFileResponse>> GetFilesAsync(CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files?pageSize=50&orderBy=modifiedTime%20desc&q=trashed%3Dfalse&fields=files(id,name,mimeType,webViewLink,thumbnailLink,modifiedTime)");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"تعذر قراءة ملفات Google Drive: {body}");
        var payload = JsonSerializer.Deserialize<DriveFilesPayload>(body, JsonOptions) ?? new DriveFilesPayload();
        return payload.Files.Select(file => new DriveFileResponse(file.Id ?? string.Empty, file.Name ?? string.Empty, file.MimeType ?? string.Empty, file.WebViewLink, file.ThumbnailLink, file.ModifiedTime)).ToList();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var connection = await db.GoogleDriveConnections.SingleOrDefaultAsync(item => item.ConnectionKey == "default", cancellationToken) ?? throw new InvalidOperationException("لم يتم ربط Google Drive بعد.");
        if (connection.AccessTokenExpiresAtUtc > DateTime.Now.AddMinutes(1)) return _tokenProtector.Unprotect(connection.AccessTokenProtected);

        var refresh = _tokenProtector.Unprotect(connection.RefreshTokenProtected);
        var refreshed = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refresh,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshed.RefreshToken)) refreshed = refreshed with { RefreshToken = refresh };
        await SaveTokenAsync(refreshed, cancellationToken);
        return refreshed.AccessToken;
    }

    private async Task SaveTokenAsync(GoogleTokenResponse token, CancellationToken cancellationToken)
    {
        var connection = await db.GoogleDriveConnections.SingleOrDefaultAsync(item => item.ConnectionKey == "default", cancellationToken);
        if (connection is null)
        {
            connection = new GoogleDriveConnectionEntity { ConnectionKey = "default" };
            db.GoogleDriveConnections.Add(connection);
        }
        connection.AccessTokenProtected = _tokenProtector.Protect(token.AccessToken);
        connection.RefreshTokenProtected = _tokenProtector.Protect(token.RefreshToken!);
        connection.AccessTokenExpiresAtUtc = DateTime.Now.AddSeconds(Math.Max(token.ExpiresIn - 30, 30));
        connection.UpdatedAtUtc = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GoogleTokenResponse> RequestTokenAsync(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token") { Content = new FormUrlEncodedContent(fields) };
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"فشل تفويض Google Drive: {body}");
        return JsonSerializer.Deserialize<GoogleTokenResponse>(body, JsonOptions) ?? throw new InvalidOperationException("رد غير صالح من Google.");
    }

    private void ValidateState(string state)
    {
        var payload = JsonSerializer.Deserialize<OAuthState>(_stateProtector.Unprotect(state), JsonOptions) ?? throw new InvalidOperationException("حالة OAuth غير صالحة.");
        if (payload.ExpiresAtUtc < DateTimeOffset.UtcNow) throw new InvalidOperationException("انتهت صلاحية جلسة OAuth. ابدأ الربط من جديد.");
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret) || string.IsNullOrWhiteSpace(_options.RedirectUri))
            throw new InvalidOperationException("إعدادات GoogleDrive غير مكتملة في الخادم.");
    }

    private static string ToQueryString(IReadOnlyDictionary<string, string?> values) => string.Join("&", values.Where(pair => pair.Value is not null).Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

    private sealed record OAuthState(DateTimeOffset ExpiresAtUtc);
    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
    private sealed record DriveFilesPayload(IReadOnlyList<DriveFilePayload> Files)
    {
        public DriveFilesPayload() : this(Array.Empty<DriveFilePayload>()) { }
    }
    private sealed record DriveFilePayload(string? Id, string? Name, string? MimeType, string? WebViewLink, string? ThumbnailLink, DateTime? ModifiedTime);
}
