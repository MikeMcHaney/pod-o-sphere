using PodOSphere.Api.Ingestion;

namespace PodOSphere.Api.Tests;

public sealed class IngestionJobTypesTests
{
    [Fact]
    public void YouTubeInventoryJobType_MatchesN8nContract()
    {
        Assert.Equal("YouTubeChannelInventory", IngestionJobTypes.YouTubeChannelInventory);
        Assert.Equal("YouTubeTranscriptIngestion", IngestionJobTypes.YouTubeTranscriptIngestion);
        Assert.Equal("Pending", ProcessingJobStatuses.Pending);
        Assert.Equal("InProgress", ProcessingJobStatuses.InProgress);
        Assert.Equal("YouTubeChannel", DataSourceTypes.YouTubeChannel);
        Assert.Equal("Demo", InventoryModes.Demo);
    }
}
