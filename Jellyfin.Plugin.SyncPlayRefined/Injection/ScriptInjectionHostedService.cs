using System.Reflection;
using System.Runtime.Loader;
using Jellyfin.Plugin.SyncPlayRefined.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public sealed class ScriptInjectionHostedService : IHostedService
{
    private const string JsInjectorScriptIdSuffix = "-client";

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

        switch (method)
        {
            case InjectionMethod.FileTransformation:
                if (!TryRegisterFileTransformation())
                {
                    _logger.LogError("File Transformation plugin was selected but is not available.");
                }

                break;
            case InjectionMethod.JavaScriptInjector:
                if (!TryRegisterJavaScriptInjector())
                {
                    _logger.LogError("JavaScript Injector plugin was selected but is not available.");
                }

                break;
            case InjectionMethod.DirectIndexHtml:
                if (!TryPatchIndexHtml())
                {
                    _logger.LogError("Direct index.html patch failed. The web client path may be missing or not writable.");
                }

                break;
            default:
                if (TryRegisterFileTransformation())
                {
                    _logger.LogInformation("Auto: using File Transformation.");
                }
                else if (TryRegisterJavaScriptInjector())
                {
                    _logger.LogInformation("Auto: using JavaScript Injector.");
                }
                else if (TryPatchIndexHtml())
                {
                    _logger.LogInformation("Auto: patched index.html on disk.");
                }
                else
                {
                    _logger.LogError(
                        "Auto: no injection path worked. Install File Transformation or JavaScript Injector, or make jellyfin-web/index.html writable.");
                }

                break;
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

    private bool TryRegisterFileTransformation()
    {
        try
        {
            var assembly = FindAssembly("Jellyfin.Plugin.FileTransformation", ".FileTransformation");
            var pluginInterface = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            var register = pluginInterface?.GetMethod("RegisterTransformation", BindingFlags.Public | BindingFlags.Static);
            if (register is null)
            {
                return false;
            }

            var payload = CreateJObject(register, new Dictionary<string, object?>
            {
                ["id"] = Plugin.Instance!.Id.ToString(),
                ["fileNamePattern"] = "index.html",
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
            var pluginInterface = assembly?.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");
            var register = pluginInterface?.GetMethod("RegisterScript", BindingFlags.Public | BindingFlags.Static);
            if (register is null)
            {
                return false;
            }

            var plugin = Plugin.Instance!;
            var payload = CreateJObject(register, new Dictionary<string, object?>
            {
                ["id"] = plugin.Id + JsInjectorScriptIdSuffix,
                ["name"] = plugin.Name,
                ["script"] = LoaderScript(),
                ["enabled"] = true,
                ["requiresAuthentication"] = false,
                ["pluginId"] = plugin.Id.ToString(),
                ["pluginName"] = plugin.Name,
                ["pluginVersion"] = plugin.Version.ToString()
            });
            if (payload is null)
            {
                return false;
            }

            var result = register.Invoke(null, [payload]);
            var ok = result is true;
            if (ok)
            {
                _logger.LogInformation("Registered loader script with JavaScript Injector.");
            }

            return ok;
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
            var pluginInterface = assembly?.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");
            var unregister = pluginInterface?.GetMethod("UnregisterAllScriptsFromPlugin", BindingFlags.Public | BindingFlags.Static);
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
                _patchedIndexHtml = html.Contains(IndexHtmlTransformer.Marker, StringComparison.Ordinal);
                return _patchedIndexHtml;
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
        if (path is null || !File.Exists(path))
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
        var webPath = _appPaths.WebPath;
        if (string.IsNullOrEmpty(webPath))
        {
            return null;
        }

        var path = Path.Combine(webPath, "index.html");
        return File.Exists(path) ? path : null;
    }

    private static string LoaderScript()
    {
        return """
            (function () {
              if (document.querySelector('script[plugin="SyncPlay Refined"]')) return;
              var s = document.createElement('script');
              s.setAttribute('plugin', 'SyncPlay Refined');
              s.src = '../SyncPlayRefined/script';
              (document.body || document.head).appendChild(s);
            })();
            """;
    }

    private static Assembly? FindAssembly(string assemblyName, string? nameContains = null)
    {
        return AssemblyLoadContext.All
            .SelectMany(ctx => ctx.Assemblies)
            .FirstOrDefault(a =>
                a.GetName().Name == assemblyName
                || (nameContains is not null && (a.FullName?.Contains(nameContains, StringComparison.Ordinal) ?? false)));
    }

    private static object? CreateJObject(MethodInfo method, Dictionary<string, object?> fields)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return null;
        }

        var paramType = parameters[0].ParameterType;
        var jObject = Activator.CreateInstance(paramType);
        if (jObject is null)
        {
            return null;
        }

        var indexer = paramType.GetProperty("Item", [typeof(string)]);
        var jValueType = paramType.Assembly.GetType("Newtonsoft.Json.Linq.JValue")
            ?? FindAssembly("Newtonsoft.Json")?.GetType("Newtonsoft.Json.Linq.JValue");
        if (indexer is null || jValueType is null)
        {
            return null;
        }

        foreach (var (key, value) in fields)
        {
            var token = Activator.CreateInstance(jValueType, [value]);
            indexer.SetValue(jObject, token, [key]);
        }

        return jObject;
    }
}
