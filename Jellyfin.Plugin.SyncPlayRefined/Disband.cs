using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace Jellyfin.Plugin.SyncPlayRefined;

internal static class Disband
{
    // ponytail: participant list is usernames, so every session of those users is kicked, including extra devices that were not in the group (those get a not-in-group warning). Upgrade: read SyncPlayManager._sessionToGroupMap.
    public static List<SessionInfo> SessionsInUsersGroups(
        IEnumerable<SessionInfo> sessions,
        IReadOnlyList<GroupInfoDto> groups,
        string? userName)
    {
        var names = MemberNames(groups.Select(g => g.Participants), userName);
        if (names.Count == 0)
        {
            return [];
        }

        return sessions.Where(s => s.UserName is not null && names.Contains(s.UserName)).ToList();
    }

    public static HashSet<string> MemberNames(IEnumerable<IEnumerable<string>> groups, string? userName)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(userName))
        {
            return names;
        }

        foreach (var participants in groups)
        {
            if (!participants.Any(p => string.Equals(p, userName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var p in participants)
            {
                names.Add(p);
            }
        }

        return names;
    }
}
