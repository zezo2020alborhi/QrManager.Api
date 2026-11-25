using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using QrManager.Api.Options;
using QrManager.Api.Services;

namespace QrManager.Api.Endpoints;

public static class GoogleDriveEndpoints
{
    public static IEndpointRouteBuilder MapGoogleDriveEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/integrations/google-drive").AddEndpointFilter<QrManager.Api.Security.AdminApiKeyEndpointFilter>();
        admin.MapGet("/authorization-url", GetAuthorizationUrl);
        admin.MapGet("/files", GetFilesAsync);
        app.MapGet("/api/integrations/google-drive/callback", CallbackAsync);
        return app;
    }

    private static Ok<object> GetAuthorizationUrl(GoogleDriveOAuthService service) => TypedResults.Ok<object>(new { url = service.CreateAuthorizationUri().ToString() });

    private static async Task<IResult> GetFilesAsync(GoogleDriveOAuthService service, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.GetFilesAsync(cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> CallbackAsync(string? code, string? state, string? error, GoogleDriveOAuthService service, IOptions<GoogleDriveOptions> options, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error)) return Results.Content(Page("تعذر ربط Google Drive", error), "text/html; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) return Results.Content(Page("بيانات تفويض غير مكتملة", "أعد بدء عملية الربط من التطبيق."), "text/html; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
        try
        {
            await service.CompleteAuthorizationAsync(code, state, cancellationToken);
            if (Uri.TryCreate(options.Value.SuccessRedirectUri, UriKind.Absolute, out var successUri)) return Results.Redirect(successUri.ToString());
            return Results.Content(Page("تم الربط بنجاح", "تم ربط Google Drive بالخادم. يمكنك العودة إلى التطبيق الآن."), "text/html; charset=utf-8");
        }
        catch (Exception exception)
        {
            return Results.Content(Page("تعذر حفظ الربط", exception.Message), "text/html; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static string Page(string title, string message) => $"<!doctype html><html lang='ar' dir='rtl'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>{System.Net.WebUtility.HtmlEncode(title)}</title><style>body{{font-family:Arial,sans-serif;background:#f7f9fc;padding:32px;color:#152033}}main{{max-width:600px;margin:auto;background:#fff;padding:28px;border-radius:18px}}p{{line-height:1.7;color:#667085}}</style><main><h1>{System.Net.WebUtility.HtmlEncode(title)}</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p></main></html>";
}
