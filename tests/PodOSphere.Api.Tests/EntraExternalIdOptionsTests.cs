using PodOSphere.Api.Configuration;

namespace PodOSphere.Api.Tests;

public sealed class EntraExternalIdOptionsTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string ClientId = "22222222-2222-2222-2222-222222222222";

    [Theory]
    [InlineData("https://example.ciamlogin.com/", $"https://example.ciamlogin.com/{TenantId}/v2.0")]
    [InlineData($"https://example.ciamlogin.com/{TenantId}", $"https://example.ciamlogin.com/{TenantId}/v2.0")]
    [InlineData($"https://example.ciamlogin.com/{TenantId}/v2.0", $"https://example.ciamlogin.com/{TenantId}/v2.0")]
    public void GetTokenAuthority_NormalizesExternalIdAuthority(string configuredAuthority, string expected)
    {
        var settings = new EntraExternalIdOptions
        {
            Authority = configuredAuthority,
            TenantId = TenantId
        };

        Assert.Equal(expected, settings.GetTokenAuthority());
    }

    [Fact]
    public void GetValidAudiences_AcceptsClientIdAndApplicationIdUri()
    {
        var settings = new EntraExternalIdOptions
        {
            ClientId = ClientId,
            Audience = $"api://{ClientId}"
        };

        Assert.Equal([ClientId, $"api://{ClientId}"], settings.GetValidAudiences());
    }
}
