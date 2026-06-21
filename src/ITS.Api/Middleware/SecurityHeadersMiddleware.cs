namespace ITS.Api.Middleware;

/// <summary>
/// Injects security-relevant HTTP response headers on every response.
/// Must be placed early in the pipeline (before UseStaticFiles / UseRouting).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent the browser from MIME-sniffing the content type
        headers["X-Content-Type-Options"] = "nosniff";

        // Deny iframe embedding from other origins (clickjacking protection)
        headers["X-Frame-Options"] = "DENY";

        // Disable legacy XSS auditor (modern browsers ignore it; older ones may act on it)
        headers["X-XSS-Protection"] = "0";

        // Control referrer information sent on cross-origin requests
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Restrict browser features; disable unused capabilities
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Enforce HTTPS for one year when the API is accessed over TLS
        // (Only set when the connection is already secure to avoid breaking HTTP → HTTPS redirect flow)
        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Conservative Content-Security-Policy for an API (no HTML rendered by the API itself)
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Remove the Server header added by Kestrel/IIS to reduce information disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        return next(context);
    }
}
