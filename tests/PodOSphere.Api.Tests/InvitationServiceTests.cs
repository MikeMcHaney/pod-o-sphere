using PodOSphere.Api.Invitations;

namespace PodOSphere.Api.Tests;

public sealed class InvitationServiceTests
{
    [Fact]
    public void HashToken_IsDeterministicAndDoesNotExposeRawToken()
    {
        const string token = "sample-token";

        var firstHash = InvitationService.HashToken(token);
        var secondHash = InvitationService.HashToken(token);

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(token, firstHash);
        Assert.Equal(64, firstHash.Length);
    }
}
