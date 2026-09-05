using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SiteRpCommand : ICommand
{
    public string Command => "siterp";
    public string[] Aliases => new[] { "srp" };
    public string Description => "Controle le coeur RP permanent SiteRP.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is not null && !player.RemoteAdminAccess)
        {
            response = "Acces refuse : Remote Admin requis.";
            return false;
        }

        if (arguments.Count == 0 || Arg(arguments, 0).Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            response = Status();
            return true;
        }

        string category = Arg(arguments, 0).ToLowerInvariant();
        string action = arguments.Count > 1 ? Arg(arguments, 1).ToLowerInvariant() : string.Empty;

        switch (category)
        {
            case "containment":
            case "confinement":
                if (action is "lock" or "on")
                    SiteRpCorePlugin.ContainmentLocked = true;
                else if (action is "unlock" or "off")
                    SiteRpCorePlugin.ContainmentLocked = false;
                else
                {
                    response = "Usage: siterp containment lock|unlock";
                    return false;
                }
                response = $"Confinement SCP: {(SiteRpCorePlugin.ContainmentLocked ? "VERROUILLE" : "OUVERT / BRECHE RP")}.";
                return true;

            case "round":
            case "permanent":
                if (!TryOnOff(action, out bool roundOn))
                {
                    response = "Usage: siterp round on|off";
                    return false;
                }
                SiteRpCorePlugin.PermanentRoundEnabled = roundOn;
                Round.IsLocked = roundOn;
                response = $"Mode RP permanent: {(roundOn ? "ACTIVE" : "DESACTIVE")}.";
                return true;

            case "waves":
                if (!TryBlockAllow(action, out bool blockWaves))
                {
                    response = "Usage: siterp waves block|allow";
                    return false;
                }
                SiteRpCorePlugin.BlockAutomaticWaves = blockWaves;
                response = $"Vagues automatiques: {(blockWaves ? "BLOQUEES" : "AUTORISEES")}.";
                return true;

            case "decon":
                if (!TryBlockAllow(action, out bool blockDecon))
                {
                    response = "Usage: siterp decon block|allow";
                    return false;
                }
                SiteRpCorePlugin.BlockDecontamination = blockDecon;
                response = $"Decontamination LCZ: {(blockDecon ? "BLOQUEE" : "AUTORISEE")}.";
                return true;

            case "warhead":
            case "ogive":
                if (!TryBlockAllow(action, out bool blockWarhead))
                {
                    response = "Usage: siterp warhead block|allow";
                    return false;
                }
                SiteRpCorePlugin.BlockWarhead = blockWarhead;
                response = $"Ogive Alpha: {(blockWarhead ? "BLOQUEE" : "AUTORISEE")}.";
                return true;

            case "escape":
            case "escapes":
                if (!TryBlockAllow(action, out bool blockEscapes))
                {
                    response = "Usage: siterp escapes block|allow";
                    return false;
                }
                SiteRpCorePlugin.BlockEscapes = blockEscapes;
                response = $"Escapes vanilla: {(blockEscapes ? "BLOQUEES" : "AUTORISEES")}.";
                return true;

            default:
                response = "Commandes: siterp status | containment lock/unlock | round on/off | waves block/allow | decon block/allow | warhead block/allow | escapes block/allow";
                return false;
        }
    }

    private static string Status() =>
        "SiteRP.Core status\n" +
        $"RP permanent: {(SiteRpCorePlugin.PermanentRoundEnabled ? "ON" : "OFF")}\n" +
        $"Confinement SCP: {(SiteRpCorePlugin.ContainmentLocked ? "LOCK" : "UNLOCK")}\n" +
        $"Vagues auto: {(SiteRpCorePlugin.BlockAutomaticWaves ? "BLOCK" : "ALLOW")}\n" +
        $"Decontamination: {(SiteRpCorePlugin.BlockDecontamination ? "BLOCK" : "ALLOW")}\n" +
        $"Ogive: {(SiteRpCorePlugin.BlockWarhead ? "BLOCK" : "ALLOW")}\n" +
        $"Escapes: {(SiteRpCorePlugin.BlockEscapes ? "BLOCK" : "ALLOW")}";

    private static string Arg(ArraySegment<string> arguments, int index) =>
        arguments.Array![arguments.Offset + index];

    private static bool TryOnOff(string value, out bool enabled)
    {
        if (value is "on" or "enable" or "enabled")
        {
            enabled = true;
            return true;
        }
        if (value is "off" or "disable" or "disabled")
        {
            enabled = false;
            return true;
        }
        enabled = false;
        return false;
    }

    private static bool TryBlockAllow(string value, out bool block)
    {
        if (value is "block" or "blocked" or "on")
        {
            block = true;
            return true;
        }
        if (value is "allow" or "allowed" or "off")
        {
            block = false;
            return true;
        }
        block = false;
        return false;
    }
}
