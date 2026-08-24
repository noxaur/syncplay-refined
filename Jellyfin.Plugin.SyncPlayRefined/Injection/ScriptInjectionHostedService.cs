using System.Reflection;
using System.Runtime.Loader;
using Jellyfin.Plugin.SyncPlayRefined.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public sealed class ScriptInjectionHostedService : IHostedService
{
    private readonly ILogger<ScriptInjectionHostedService> _logger;
    private readonly IApplicationPaths _appPaths;
    private bool _patchedIndexHtml;

    public ScriptInjectionHostedService(
        ILogger<ScriptInjectionHostedService> logger,
        IApplicationPaths appPaths)
    {
        _logger = logger;
        _appPaths = appPaths;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is null)
        {
            _logger.LogError("Plugin instance is not available; cannot inject client script.");
            return Task.CompletedTask;
        }

        var method = Plugin.Instance.Configuration.InjectionMethod;
        _logger.LogInformation("SyncPlay Refined injection method: {Method}", method);

        var ok = method switch
        {
            InjectionMethod.FileTransformation => TryRegisterFileTransformation(),
            InjectionMethod.JavaScriptInjector => TryRegisterJavaScriptInjector(),
            InjectionMethod.DirectIndexHtml => TryPatchIndexHtml(),
            _ => RegisterAuto()
        };
        if (!ok)
        {
            _logger.LogError(method switch
            {
                InjectionMethod.FileTransformation => "File Transformation plugin was selected but is not available.",
                InjectionMethod.JavaScriptInjector => "JavaScript Injector plugin was selected but is not available.",
                InjectionMethod.DirectIndexHtml => "Direct index.html patch failed. The web client path may be missing or not writable.",
                _ => "Auto: no injection path worked. Install File Transformation or JavaScript Injector, or make jellyfin-web/index.html writable."
            });
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is not null)
        {
            TryUnregisterJavaScriptInjector();
        }

        if (_patchedIndexHtml)
        {
            TryUnpatchIndexHtml();
        }

        return Task.CompletedTask;
    }

    private bool RegisterAuto()
    {
        var ft = TryRegisterFileTransformation();
        var js = TryRegisterJavaScriptInjector();
        if (ft || js)
        {
            return true;
        }

        return TryPatchIndexHtml();
    }

    private bool TryRegisterFileTransformation()
    {
        try
        {
            var assembly = FindAssembly("Jellyfin.Plugin.FileTransformation", ".FileTransformation");
            var register = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")
                ?.GetMethod("RegisterTransformation", BindingFlags.Public | BindingFlags.Static);
            if (register is null)
            {
                return false;
            }

            var payload = CreateJObject(register, new Dictionary<string, object?>
            {
                ["id"] = Plugin.Instance!.Id.ToString(),
                ["fileNamePattern"] = IndexHtmlTransformer.FileNamePattern,
                ["callbackAssembly"] = typeof(IndexHtmlTransformer).Assembly.FullName,
                ["callbackClass"] = typeof(IndexHtmlTransformer).FullName,
                ["callbackMethod"] = nameof(IndexHtmlTransformer.TransformIndexHtml)
            });
            if (payload is null)
            {
                return false;
            }

            register.Invoke(null, [payload]);
            _logger.LogInformation("Registered index.html transform with File Transformation.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "File Transformation registration failed.");
            return false;
        }
    }

    private bool TryRegisterJavaScriptInjector()
    {
        try
        {
            var assembly = FindAssembly("Jellyfin.Plugin.JavaScriptInjector");
            var register = assembly?.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface")
                ?.GetMethod("RegisterScript", BindingFlags.Public | BindingFlags.Static);
            if (register is null)
            {
                return false;
            }

            var plugin = Plugin.Instance!;
            var payload = CreateJObject(register, new Dictionary<string, object?>
            {
                ["id"] = plugin.Id + "-client",
                ["name"] = plugin.Name,
                ["script"] = LoaderScript(),
                ["enabled"] = true,
                ["requiresAuthentication"] = plugin.Configuration.RequiresAuthentication,
                ["pluginId"] = plugin.Id.ToString(),
                ["pluginName"] = plugin.Name,
                ["pluginVersion"] = plugin.Version.ToString()
            });
            if (payload is null)
            {
                return false;
            }

            if (register.Invoke(null, [payload]) is not true)
            {
                return false;
            }

            _logger.LogInformation("Registered loader script with JavaScript Injector.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JavaScript Injector registration failed.");
            return false;
        }
    }

    private void TryUnregisterJavaScriptInjector()
    {
        try
        {
            var assembly = FindAssembly("Jellyfin.Plugin.JavaScriptInjector");
            var unregister = assembly?.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface")
                ?.GetMethod("UnregisterAllScriptsFromPlugin", BindingFlags.Public | BindingFlags.Static);
            unregister?.Invoke(null, [Plugin.Instance!.Id.ToString()]);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "JavaScript Injector unregister failed.");
        }
    }

    private bool TryPatchIndexHtml()
    {
        var path = GetIndexHtmlPath();
        if (path is null)
        {
            return false;
        }

        try
        {
            var html = File.ReadAllText(path);
            var updated = IndexHtmlTransformer.Inject(html);
            if (updated == html)
            {
                return _patchedIndexHtml = html.Contains(IndexHtmlTransformer.Marker, StringComparison.Ordinal);
            }

            File.WriteAllText(path, updated);
            _patchedIndexHtml = true;
            _logger.LogInformation("Wrote script tag to {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write {Path}", path);
            return false;
        }
    }

    private void TryUnpatchIndexHtml()
    {
        var path = GetIndexHtmlPath();
        if (path is null)
        {
            return;
        }

        try
        {
            var html = File.ReadAllText(path);
            var updated = IndexHtmlTransformer.Strip(html);
            if (updated != html)
            {
                File.WriteAllText(path, updated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not revert {Path}", path);
        }
    }

    private string? GetIndexHtmlPath()
    {
        if (string.IsNullOrEmpty(_appPaths.WebPath))
        {
            return null;
        }

        var path = Path.Combine(_appPaths.WebPath, "index.html");
        return File.Exists(path) ? path : null;
    }

    private static string LoaderScript() => """
            (function () {
              var src = '__SRC__';
              var existing = document.querySelector('script[plugin="SyncPlay Refined"]');
              if (existing && (existing.getAttribute('src') || '') === src) return;
              if (existing) existing.remove();
              var s = document.createElement('script');
              s.setAttribute('plugin', 'SyncPlay Refined');
              s.src = src;
              (document.body || document.head).appendChild(s);
            })();
            """.Replace("__SRC__", IndexHtmlTransformer.ScriptSrc, StringComparison.Ordinal);

    private static Assembly? FindAssembly(string assemblyName, string? nameContains = null) =>
        AssemblyLoadContext.All
            .SelectMany(ctx => ctx.Assemblies)
            .FirstOrDefault(a =>
                a.GetName().Name == assemblyName
                || (nameContains is not null && (a.FullName?.Contains(nameContains, StringComparison.Ordinal) ?? false)));

    private static object? CreateJObject(MethodInfo method, Dictionary<string, object?> fields)
    {
        if (method.GetParameters() is not [{ ParameterType: var paramType }, ..])
        {
            return null;
        }

        var jObject = Activator.CreateInstance(paramType);
        var indexer = paramType.GetProperty("Item", [typeof(string)]);
        var jValueType = paramType.Assembly.GetType("Newtonsoft.Json.Linq.JValue")
            ?? FindAssembly("Newtonsoft.Json")?.GetType("Newtonsoft.Json.Linq.JValue");
        if (jObject is null || indexer is null || jValueType is null)
        {
            return null;
        }

        foreach (var (key, value) in fields)
        {
            indexer.SetValue(jObject, Activator.CreateInstance(jValueType, [value]), [key]);
        }

        return jObject;
    }
}
