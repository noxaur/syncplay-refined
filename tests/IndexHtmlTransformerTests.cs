using System.Text.RegularExpressions;
using Jellyfin.Plugin.SyncPlayRefined.Injection;
using Xunit;

namespace Jellyfin.Plugin.SyncPlayRefined.Tests;

public class IndexHtmlTransformerTests
{
    private static readonly string LoginChunk =
        "session-login-index-html.7df1620bd3afcef60eb7.chunk.js";

    [Fact]
    public void FileNamePattern_matches_index_html_only()
    {
        var re = new Regex(IndexHtmlTransformer.FileNamePattern);
        Assert.Matches(re, "index.html");
        Assert.Matches(re, "/jellyfin/jellyfin-web/index.html");
        Assert.DoesNotMatch(re, LoginChunk);
        Assert.DoesNotMatch(re, "session-login-index-html.chunk.js");
        Assert.DoesNotMatch(re, "settings-index-html.eb37b8107cd35488a326.chunk.js");
    }

    [Fact]
    public void Unescaped_index_html_regex_matches_the_login_chunk()
    {
        Assert.Matches("index.html", LoginChunk);
    }

    [Fact]
    public void Inject_leaves_javascript_chunks_unchanged()
    {
        var js = "!function(){console.log(\"login\")}();";
        Assert.Equal(js, IndexHtmlTransformer.Inject(js));
    }

    [Fact]
    public void Inject_inserts_script_before_body_close()
    {
        var html = "<html><body><div id=\"reactRoot\"></div></body></html>";
        var result = IndexHtmlTransformer.Inject(html);
        Assert.Contains("?v=", IndexHtmlTransformer.ScriptTag, StringComparison.Ordinal);
        Assert.Contains(IndexHtmlTransformer.PluginVersion, IndexHtmlTransformer.ScriptSrc, StringComparison.Ordinal);
        Assert.Contains("</div>" + IndexHtmlTransformer.ScriptTag + "\n</body>", result, StringComparison.Ordinal);
        Assert.Equal(result, IndexHtmlTransformer.Inject(result));
    }

    [Fact]
    public void Inject_replaces_legacy_unversioned_script_tag()
    {
        var html = "<html><body><div></div>\n<script plugin=\"SyncPlay Refined\" src=\"../SyncPlayRefined/script\"></script>\n</body></html>";
        var result = IndexHtmlTransformer.Inject(html);
        Assert.Contains(IndexHtmlTransformer.ScriptTag, result, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"../SyncPlayRefined/script\"", result, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(result, IndexHtmlTransformer.Marker));
    }

    [Fact]
    public void Strip_removes_versioned_and_legacy_tags()
    {
        var legacy = "<html><body><script plugin=\"SyncPlay Refined\" src=\"../SyncPlayRefined/script\"></script></body></html>";
        Assert.DoesNotContain(IndexHtmlTransformer.Marker, IndexHtmlTransformer.Strip(legacy), StringComparison.Ordinal);

        var current = "<html><body>" + IndexHtmlTransformer.ScriptTag + "</body></html>";
        Assert.DoesNotContain(IndexHtmlTransformer.Marker, IndexHtmlTransformer.Strip(current), StringComparison.Ordinal);
    }
}
