namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public static class IndexHtmlTransformer
{
    public const string Marker = "plugin=\"SyncPlay Refined\"";
    public const string ScriptTag = "<script plugin=\"SyncPlay Refined\" src=\"../SyncPlayRefined/script\"></script>";

    public static string Inject(string html)
    {
        if (html.Contains(Marker, StringComparison.Ordinal))
        {
            return html;
        }

        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return bodyClose < 0 ? html + ScriptTag : html.Insert(bodyClose, ScriptTag + "\n");
    }

    public static string Strip(string html)
    {
        if (!html.Contains(Marker, StringComparison.Ordinal))
        {
            return html;
        }

        return html.Replace(ScriptTag + "\n", string.Empty, StringComparison.Ordinal)
            .Replace(ScriptTag, string.Empty, StringComparison.Ordinal);
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
