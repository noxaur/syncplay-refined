using Jellyfin.Plugin.SyncPlayRefined.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SyncPlayRefined.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void EnableDevFeatures_defaults_off()
    {
        Assert.False(new PluginConfiguration().EnableDevFeatures);
    }
}
