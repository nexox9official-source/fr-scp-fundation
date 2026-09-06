using System.Collections;
using System.Reflection;
using MEC;

namespace SiteRP.Core;

/// <summary>
/// Runtime bridge between UncomplicatedCustomTeams and UCR without a hard plugin dependency.
/// If a player belongs to a UCT team, the visible UCR role line is replaced by the custom team name.
/// </summary>
internal static class SiteRpCustomTeamDisplay
{
    public static void ScheduleRefresh(Player player)
    {
        if (player is null)
            return;

        Timing.CallDelayed(0.45f, () => Apply(player));
        Timing.CallDelayed(1.35f, () => Apply(player));
    }

    private static void Apply(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        try
        {
            string? teamName = FindCustomTeamName(player);
            if (string.IsNullOrWhiteSpace(teamName))
                return;

            object? summonedRole = FindUcrSummonedRole(player);
            if (summonedRole is null)
                return;

            object? customInfo = summonedRole.GetType().GetProperty("CustomInfo", BindingFlags.Instance | BindingFlags.Public)?.GetValue(summonedRole);
            PropertyInfo? roleProperty = customInfo?.GetType().GetProperty("Role", BindingFlags.Instance | BindingFlags.Public);
            if (customInfo is null || roleProperty is null || !roleProperty.CanWrite)
                return;

            string current = roleProperty.GetValue(customInfo)?.ToString() ?? string.Empty;
            if (string.Equals(current, teamName, StringComparison.Ordinal))
                return;

            roleProperty.SetValue(customInfo, teamName);
            Logger.Info($"[SiteRP Teams] {player.Nickname}: role visible '{current}' -> team custom '{teamName}'.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SiteRP Teams] Impossible d'appliquer le nom de team custom a {player.Nickname}: {ex.GetBaseException().Message}");
        }
    }

    private static string? FindCustomTeamName(Player player)
    {
        Type? type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UncomplicatedCustomTeams.API.Features.Runtime.SummonedTeam", false))
            .FirstOrDefault(t => t is not null);
        if (type is null)
            return null;

        object? list = type.GetProperty("List", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (list is not IEnumerable teams)
            return null;

        foreach (object? team in teams)
        {
            if (team is null)
                continue;

            object? membersObj = team.GetType().GetProperty("Members", BindingFlags.Public | BindingFlags.Instance)?.GetValue(team);
            if (membersObj is not IEnumerable members)
                continue;

            bool found = false;
            foreach (object? member in members)
            {
                object? memberPlayer = member?.GetType().GetProperty("Player", BindingFlags.Public | BindingFlags.Instance)?.GetValue(member);
                if (ReferenceEquals(memberPlayer, player))
                {
                    found = true;
                    break;
                }

                if (memberPlayer is Player wrapper && wrapper.PlayerId == player.PlayerId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                continue;

            object? definition = team.GetType().GetProperty("Definition", BindingFlags.Public | BindingFlags.Instance)?.GetValue(team);
            return definition?.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(definition)?.ToString();
        }

        return null;
    }

    private static object? FindUcrSummonedRole(Player player)
    {
        Type? type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UncomplicatedCustomRoles.API.Features.SummonedCustomRole", false))
            .FirstOrDefault(t => t is not null);
        if (type is null)
            return null;

        MethodInfo? tryGet = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "TryGet" && m.GetParameters().Length == 2 && m.GetParameters()[1].IsOut && m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Player)));
        if (tryGet is null)
            return null;

        object?[] args = { player, null };
        object? result = tryGet.Invoke(null, args);
        return result is true ? args[1] : null;
    }
}
