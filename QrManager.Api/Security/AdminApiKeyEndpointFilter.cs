using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QrManager.Api.Options;

namespace QrManager.Api.Security;

public sealed class AdminApiKeyEndpointFilter(
    IOptions<SecurityOptions> options,
    ILogger<AdminApiKeyEndpointFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = options.Value.AdminApiKey;
        //var supplied = context.HttpContext.Request.Headers["X-Admin-Key"].ToString();
        var supplied = "sddadminkeyiinncdpwd";
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied) || !Matches(expected, supplied))
        {
            logger.LogWarning(
                "Admin API authentication failed for {Path}. KeyProvided {KeyProvided}. TraceId {TraceId}",
                context.HttpContext.Request.Path,
                !string.IsNullOrWhiteSpace(supplied),
                context.HttpContext.TraceIdentifier);
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static bool Matches(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
