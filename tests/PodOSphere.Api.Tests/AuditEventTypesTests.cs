using PodOSphere.Api.Auditing;

namespace PodOSphere.Api.Tests;

public sealed class AuditEventTypesTests
{
    [Fact]
    public void EventTypeConstants_AreStableMachineNames()
    {
        Assert.Equal("superadmin.tenants.viewed", AuditEventTypes.SuperAdminTenantsViewed);
        Assert.Equal("invitation.created", AuditEventTypes.InvitationCreated);
        Assert.Equal("invitation.accepted", AuditEventTypes.InvitationAccepted);
        Assert.Equal("invitation.revoked", AuditEventTypes.InvitationRevoked);
        Assert.Equal("show_claim.submitted", AuditEventTypes.ShowClaimSubmitted);
        Assert.Equal("show_claim.approved", AuditEventTypes.ShowClaimApproved);
        Assert.Equal("show_claim.rejected", AuditEventTypes.ShowClaimRejected);
    }
}
