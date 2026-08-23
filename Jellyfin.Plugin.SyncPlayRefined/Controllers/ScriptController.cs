using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SyncPlayRefined.Controllers;

[ApiController]
[Route("SyncPlayRefined")]
public class ScriptController : ControllerBase
{
    [HttpGet("script")]
    [AllowAnonymous]
    public ActionResult GetScript()
    {
        var assembly = typeof(Plugin).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Web.client.js", StringComparison.Ordinal));
        using var stream = name is null ? null : assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        Response.Headers.CacheControl = "no-store";
        var config = Plugin.Instance?.Configuration;
        var requireAuth = config?.RequiresAuthentication ?? true;
        var enableDev = config?.EnableDevFeatures ?? false;
        return Content(
            "window.__syncPlayRefinedRequireAuth=" + (requireAuth ? "true" : "false") +
            ";window.__syncPlayRefinedDev=" + (enableDev ? "true" : "false") + ";\n" +
            reader.ReadToEnd(),
            "text/javascript");
    }
}
