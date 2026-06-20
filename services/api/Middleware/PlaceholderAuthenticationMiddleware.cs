using System.Security.Claims;

namespace PodOSphere.Api.Middleware;

public sealed class PlaceholderAuthenticationMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Development-only identity makes the future Entra boundary visible without simulating authorization.
        if (environment.IsDevelopment() &&
            context.Request.Headers.TryGetValue("X-Development-User", out var userName) &&
            !string.IsNullOrWhiteSpace(userName))
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, userName.ToString())],
                authenticationType: "DevelopmentPlaceholder");

            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }
}

