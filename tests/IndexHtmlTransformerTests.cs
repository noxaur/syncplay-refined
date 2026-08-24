using System.Text.RegularExpressions;
using Jellyfin.Plugin.SyncPlayRefined.Injection;
using Xunit;

namespace Jellyfin.Plugin.SyncPlayRefined.Tests;

public class IndexHtmlTransformerTests
{
    private static readonly string LoginChunk =
        "session-login-index-html.7df1620bd3afcef60eb7.chunk.js";

    [Fact]
    public void FileNamePattern_is_the_file_transformation_dictionary_key()
    {
        Assert.Equal("index.html", IndexHtmlTransformer.FileNamePattern);
    }

    [Fact]
    public void FileTransformation_skips_a_regex_only_key_when_index_html_is_already_registered()
    {
        Assert.Equal(
            "index.html",
            FileTransformationPipelineKey("index.html", "index.html", @"index\.html$"));
        Assert.Equal(
            "index.html",
            FileTransformationPipelineKey("/index.html", "index.html", @"index\.html$"));
        Assert.Equal(
            @"index\.html$",
            FileTransformationPipelineKey("index.html", @"index\.html$"));
        Assert.NotEqual(
            @"index\.html$",
            FileTransformationPipelineKey("index.html", "index.html", @"index\.html$"));
    }

    [Fact]
    public void Unescaped_index_html_regex_matches_the_login_chunk()
    {
        Assert.Matches("index.html", LoginChunk);
        Assert.Equal("index.html", FileTransformationPipelineKey(LoginChunk, "index.html"));
    }

    [Fact]
    public void Inject_leaves_javascript_chunks_unchanged()
    {
        var js = "!function(){console.log(\"login\")}();";
        Assert.Equal(js, IndexHtmlTransformer.Inject(js));
    }

    [Fact]
    public void Inject_leaves_javascript_that_embeds_body_markup_unchanged()
    {
        var js = "!function(){document.write('</body></html>')}();";
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

    // Mirrors File Transformation 2.5.x WebFileTransformationService.RunTransformation:
    // exact dictionary key first, regex fallback only if that key is missing.
    private static string? FileTransformationPipelineKey(string requestPath, params string[] registeredKeys)
    {
        var keys = registeredKeys.Select(k => k.TrimStart('/')).ToArray();
        var path = requestPath.TrimStart('/');
        if (keys.Contains(path))
        {
            return path;
        }

        return keys.FirstOrDefault(key => Regex.IsMatch(path, key));
    }
}
