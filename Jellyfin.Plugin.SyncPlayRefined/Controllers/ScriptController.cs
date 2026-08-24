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
        NoStore();
        var flags = CurrentFlags();
        return Content(
            "window.__syncPlayRefinedRequireAuth=" + (flags.RequiresAuthentication ? "true" : "false") +
            ";window.__syncPlayRefinedDev=" + (flags.EnableDevFeatures ? "true" : "false") +
            ";window.__syncPlayRefinedDisbandGroup=" + (flags.DisbandGroup ? "true" : "false") + ";\n" +
            reader.ReadToEnd(),
            "text/javascript");
    }

    [HttpGet("flags")]
    [AllowAnonymous]
    public ActionResult GetFlags()
    {
        NoStore();
        return Ok(CurrentFlags());
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

    private void NoStore()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
    }

    private static Flags CurrentFlags()
    {
        var config = Plugin.Instance?.Configuration;
        return new Flags(
            config?.RequiresAuthentication ?? true,
            config?.DisbandGroup == true,
            config?.EnableDevFeatures == true);
    }

    private sealed record Flags(bool RequiresAuthentication, bool DisbandGroup, bool EnableDevFeatures);

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
