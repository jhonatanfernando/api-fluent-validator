using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;

namespace ApiFluentValidator.Middleware;

public class CustomApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    private readonly List<string> _apiKeys;


    public CustomApiKeyMiddleware(RequestDelegate next, string apiKey)
        : this(next, new List<string> { apiKey })
    {
    }

    public CustomApiKeyMiddleware(RequestDelegate next, List<string> apiKeys)
    {
        _next = next;
        _apiKeys = apiKeys;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string header = context.Request.Headers[Constants.ApiKeyHeaderName].ToString();

        if ((context.Features.Get<IEndpointFeature>()?.Endpoint?.Metadata.Any((m) => m is AllowAnonymousAttribute)).GetValueOrDefault() || 
            !string.IsNullOrWhiteSpace(header) && _apiKeys.Any((k) => k == header))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = 401;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("ApiKey is invalid.");
    }
}
