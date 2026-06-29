using Microsoft.Extensions.Options;
using PodOSphere.Api.Configuration;

namespace PodOSphere.Api.Ingestion;

public static class InternalIngestionAuth
{
    public const string HeaderName = "X-PodOSphere-Internal-Token";

    public static IResult? Validate(HttpRequest request, IOptions<PodOSphereOptions> options)
    {
        var configuredToken = options.Value.InternalIngestionToken;
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            return Results.Problem("Internal ingestion token is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var providedToken = request.Headers[HeaderName].ToString();
        return string.Equals(providedToken, configuredToken, StringComparison.Ordinal)
            ? null
            : Results.Unauthorized();
    }
}
