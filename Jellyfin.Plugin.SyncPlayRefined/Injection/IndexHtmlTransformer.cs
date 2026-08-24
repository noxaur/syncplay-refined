using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public static class IndexHtmlTransformer
{
    public const string Marker = "plugin=\"SyncPlay Refined\"";

    // File Transformation 2.5.x stores this as a dictionary key and only
    // regex-matches when that exact key is missing. PluginPages, JavaScript
    // Injector, and Jellyfin Enhanced all register "index.html", so a
    // regex-only key like index\.html$ never runs on a server that already
    // has those. The literal key joins their pipeline. Inject() must still
    // refuse JS because FT will regex-fallback this key against
    // session-login-index-html.*.chunk.js.
    public const string FileNamePattern = "index.html";

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
        if (!LooksLikeHtmlDocument(html))
        {
            return html;
        }

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

    // ponytail: prefix sniff. A webpack chunk that started with <!DOCTYPE or
    // <html would still get a tag. Upgrade if FT ever passes the request path
    // into the callback so we can require a real index.html name.
    private static bool LooksLikeHtmlDocument(string html)
    {
        var t = html.AsSpan().TrimStart();
        return t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
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
