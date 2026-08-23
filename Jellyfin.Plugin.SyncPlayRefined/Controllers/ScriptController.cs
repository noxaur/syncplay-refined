using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SyncPlayRefined.Controllers;

[ApiController]
[Route("SyncPlayRefined")]
public class ScriptController : ControllerBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ISyncPlayManager _syncPlayManager;

    public ScriptController(ISessionManager sessionManager, ISyncPlayManager syncPlayManager)
    {
        _sessionManager = sessionManager;
        _syncPlayManager = syncPlayManager;
    }

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
        var requireAuth = Plugin.Instance?.Configuration.RequiresAuthentication ?? true;
        var disband = Plugin.Instance?.Configuration.DisbandGroup == true;
        var preamble = "window.__syncPlayRefinedRequireAuth=" + (requireAuth ? "true" : "false")
            + ";window.__syncPlayRefinedDisbandGroup=" + (disband ? "true" : "false") + ";\n";
        return Content(preamble + reader.ReadToEnd(), "text/javascript");
    }

    [HttpPost("Disband")]
    [Authorize]
    public ActionResult DisbandGroup()
    {
        var session = CurrentSession();
        if (session is null)
        {
            return Unauthorized();
        }

        var leave = new LeaveGroupRequest();
        var ct = HttpContext.RequestAborted;
        if (Plugin.Instance?.Configuration.DisbandGroup == true)
        {
            var groups = _syncPlayManager.ListGroups(session, new ListGroupsRequest());
            var targets = Disband.SessionsInUsersGroups(_sessionManager.Sessions, groups, session.UserName);
            if (targets.Count == 0)
            {
                targets = [session];
            }

            foreach (var other in targets)
            {
                _syncPlayManager.LeaveGroup(other, leave, ct);
            }
        }
        else
        {
            _syncPlayManager.LeaveGroup(session, leave, ct);
        }

        return NoContent();
    }

    private SessionInfo? CurrentSession()
    {
        var deviceId = User.FindFirst("Jellyfin-DeviceId")?.Value
            ?? Request.Headers["X-Emby-Device-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(deviceId))
        {
            return null;
        }

        return _sessionManager.Sessions.FirstOrDefault(s =>
            string.Equals(s.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
    }
}
