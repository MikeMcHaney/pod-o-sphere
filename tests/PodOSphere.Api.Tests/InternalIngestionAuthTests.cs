using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Ingestion;

namespace PodOSphere.Api.Tests;

public sealed class InternalIngestionAuthTests
{
    [Fact]
    public void Validate_ReturnsNull_WhenHeaderMatchesConfiguredToken()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[InternalIngestionAuth.HeaderName] = "secret";
        var options = Options.Create(new PodOSphereOptions { InternalIngestionToken = "secret" });

        Assert.Null(InternalIngestionAuth.Validate(request, options));
    }

    [Fact]
    public void Validate_ReturnsProblem_WhenTokenIsNotConfigured()
    {
        var request = new DefaultHttpContext().Request;
        var options = Options.Create(new PodOSphereOptions());

        Assert.NotNull(InternalIngestionAuth.Validate(request, options));
    }
}
