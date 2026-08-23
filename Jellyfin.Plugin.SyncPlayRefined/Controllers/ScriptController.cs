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
        if (name is null)
        {
            return NotFound();
        }

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        var js = reader.ReadToEnd();
        Response.Headers.CacheControl = "no-store";
        return Content(js, "text/javascript");
    }
}
