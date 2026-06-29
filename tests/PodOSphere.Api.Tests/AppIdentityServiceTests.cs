using System.Security.Claims;
using PodOSphere.Api.Identity;

namespace PodOSphere.Api.Tests;

public sealed class AppIdentityServiceTests
{
    [Fact]
    public void GetTokenIdentity_SeparatesEmailFromPreferredUsername()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("iss", "https://tenant.example/v2.0"),
            new Claim("sub", "subject-1"),
            new Claim("name", "Test User"),
            new Claim("preferred_username", "generated@example.onmicrosoft.com")
        ]));

        var identity = AppIdentityService.GetTokenIdentity(principal);

        Assert.NotNull(identity);
        Assert.Null(identity.Email);
        Assert.Equal("generated@example.onmicrosoft.com", identity.PreferredUsername);
    }

    [Fact]
    public void GetTokenIdentity_RequiresIssuerAndSubject()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("iss", "https://tenant.example/v2.0")
        ]));

        Assert.Null(AppIdentityService.GetTokenIdentity(principal));
    }
}
