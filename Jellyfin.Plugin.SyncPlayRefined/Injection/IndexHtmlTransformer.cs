using System.Reflection;

namespace Jellyfin.Plugin.SyncPlayRefined.Injection;

public sealed class PatchRequestPayload
{
    public string? Contents { get; set; }
}

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
        if (bodyClose < 0)
        {
            return html + ScriptTag;
        }

        return html.Insert(bodyClose, ScriptTag + "\n");
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
        switch (payload)
        {
            case null:
                return string.Empty;
            case string html:
                return html;
            case PatchRequestPayload typed:
                return typed.Contents ?? string.Empty;
        }

        var type = payload.GetType();
        var prop = type.GetProperty("Contents") ?? type.GetProperty("contents");
        if (prop?.GetValue(payload) is string contents)
        {
            return contents;
        }

        var indexer = type.GetProperty("Item", [typeof(string)]);
        if (indexer?.GetValue(payload, ["contents"]) is { } token)
        {
            return token.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}

