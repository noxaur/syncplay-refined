using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SyncPlayRefined.Configuration;

public enum InjectionMethod
{
    Auto = 0,
    FileTransformation = 1,
    JavaScriptInjector = 2,
    DirectIndexHtml = 3
}

public class PluginConfiguration : BasePluginConfiguration
{
    public InjectionMethod InjectionMethod { get; set; } = InjectionMethod.Auto;
}
