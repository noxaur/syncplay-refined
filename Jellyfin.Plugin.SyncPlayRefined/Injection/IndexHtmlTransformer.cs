using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public static class IndexHtmlTransformer
{
    public const string Marker = "plugin=\"SyncPlay Refined\"";

    // File Transformation compiles this as a regex. Escape the dot so we
    // match index.html and not session-login-index-html.*.chunk.js.
    public const string FileNamePattern = @"index\.html$";

    private static readonly Regex InjectedScript = new(
        @"<script[^>]*plugin=""SyncPlay Refined""[^>]*>\s*</script>\n?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string PluginVersion =>
        typeof(IndexHtmlTransformer).Assembly.GetName().Version?.ToString() ?? "0";

    public static string ScriptSrc => $"../SyncPlayRefined/script?v={PluginVersion}";

    public static string ScriptTag =>
        $@"<script plugin=""SyncPlay Refined"" src=""{ScriptSrc}""></script>";

    public static string Inject(string html)
    {
        if (html.Contains(Marker, StringComparison.Ordinal))
        {
            // Prod index.html may still have an unversioned tag from an older
            // build. Leaving it would keep browsers on the cached script URL.
            return InjectedScript.Replace(html, ScriptTag + "\n", 1);
        }

        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyClose < 0)
        {
            return html;
        }

        return html.Insert(bodyClose, ScriptTag + "\n");
    }

    public static string Strip(string html)
    {
        if (!html.Contains(Marker, StringComparison.Ordinal))
        {
            return html;
        }

        return InjectedScript.Replace(html, string.Empty);
    }

    public static string TransformIndexHtml(object? payload)
    {
        return Inject(ReadContents(payload));
    }

    private static string ReadContents(object? payload)
    {
        if (payload is string html)
        {
            return html;
        }

        var type = payload?.GetType();
        if (type is null)
        {
            return string.Empty;
        }

        if ((type.GetProperty("Contents") ?? type.GetProperty("contents"))?.GetValue(payload) is string contents)
        {
            return contents;
        }

        return type.GetProperty("Item", [typeof(string)])?.GetValue(payload, ["contents"])?.ToString() ?? string.Empty;
    }
}
