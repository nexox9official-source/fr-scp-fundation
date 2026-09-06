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

            case "clean":
            case "cleanmap":
                if (action is "run" or "apply")
                {
                    int removed = SiteRpCorePlugin.CleanupDecorativeRagdolls("manual clean");
                    response = $"SiteRP Clean applique: {removed} ragdoll(s) reseau retire(s).";
                    return true;
                }
                if (!TryOnOff(action, out bool cleanOn))
                {
                    response = "Usage: siterp clean on|off|run";
                    return false;
                }
                SiteRpCorePlugin.CleanDecorativeRagdolls = cleanOn;
                response = $"Nettoyage des ragdolls reseau au chargement: {(cleanOn ? "ACTIVE" : "DESACTIVE")}.";
                return true;

            case "blood":
            case "sang":
                if (!TryBlockAllow(action, out bool blockBlood))
                {
                    response = "Usage: siterp blood block|allow";
                    return false;
                }
                SiteRpCorePlugin.BlockBloodDecals = blockBlood;
                response = $"Nouvelles traces de sang: {(blockBlood ? "BLOQUEES" : "AUTORISEES")}.";
                return true;

            case "map":
            case "mapping":
                return HandleMap(arguments, player, action, out response);

            default:
                response = "Commandes: siterp status | containment lock/unlock | round on/off | waves block/allow | decon block/allow | warhead block/allow | escapes block/allow | clean on/off/run | blood block/allow | map operational/audit/where/mark/report/targets/auto";
                return false;
        }
    }

    private static bool HandleMap(ArraySegment<string> arguments, Player? player, string action, out string response)
    {
        switch (action)
        {
            case "operational":
            case "op":
                if (arguments.Count < 3)
                {
                    response = $"SiteRP Operational: {(SiteRpCorePlugin.OperationalMapEnabled ? "ON" : "OFF")} | applique={(SiteRpOperationalMap.IsApplied ? "OUI" : "NON")} | elements={SiteRpOperationalMap.SpawnedCount}\nUsage: siterp map operational on|off|reload";
                    return true;
                }

                string opAction = Arg(arguments, 2).ToLowerInvariant();
                if (opAction is "reload" or "reapply")
                {
                    SiteRpCorePlugin.OperationalMapEnabled = true;
                    response = SiteRpOperationalMap.Reload();
                    return true;
                }

                if (!TryOnOff(opAction, out bool operationalOn))
                {
                    response = "Usage: siterp map operational on|off|reload";
                    return false;
                }

                SiteRpCorePlugin.OperationalMapEnabled = operationalOn;
                response = operationalOn ? SiteRpOperationalMap.Apply() : SiteRpOperationalMap.Remove();
                return true;

            case "audit":
            case "scan":
                response = "Audit genere: " + SiteRpMapSurvey.WriteAudit();
                return true;

            case "where":
            case "pos":
            case "position":
                if (player is null)
                {
                    response = "Cette sous-commande doit etre executee par un joueur en Remote Admin.";
                    return false;
                }
                response = SiteRpMapSurvey.DescribePosition(player);
                return true;

            case "mark":
            case "marker":
                if (player is null)
                {
                    response = "Cette sous-commande doit etre executee par un joueur en Remote Admin.";
                    return false;
                }
                string label = arguments.Count > 2 ? JoinFrom(arguments, 2) : "unnamed";
                response = SiteRpMapSurvey.AppendMarker(player, label);
                return true;

            case "report":
            case "last":
                response = string.IsNullOrEmpty(SiteRpMapSurvey.LastReportPath)
                    ? "Aucun rapport dans cette session. Utilise: siterp map audit"
                    : "Dernier rapport: " + SiteRpMapSurvey.LastReportPath;
                return true;

            case "targets":
                response = "Salles auditees en detail: " + SiteRpMapSurvey.TargetRoomNames;
                return true;

            case "auto":
                if (arguments.Count < 3 || !TryOnOff(Arg(arguments, 2).ToLowerInvariant(), out bool auditOn))
                {
                    response = "Usage: siterp map auto on|off";
                    return false;
                }
                SiteRpCorePlugin.AutomaticMapAudit = auditOn;
                response = $"Audit automatique de la map: {(auditOn ? "ACTIVE" : "DESACTIVE")}.";
                return true;

            default:
                response = "Usage: siterp map operational on/off/reload | map audit | map where | map mark <nom> | map report | map targets | map auto on/off";
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
        $"Escapes: {(SiteRpCorePlugin.BlockEscapes ? "BLOCK" : "ALLOW")}\n" +
        $"Ragdolls reseau au chargement: {(SiteRpCorePlugin.CleanDecorativeRagdolls ? "CLEAN" : "VANILLA")}\n" +
        $"Traces de sang nouvelles: {(SiteRpCorePlugin.BlockBloodDecals ? "BLOCK" : "ALLOW")}\n" +
        $"SiteRP Operational: {(SiteRpCorePlugin.OperationalMapEnabled ? "ON" : "OFF")} / {(SiteRpOperationalMap.IsApplied ? "APPLIQUE" : "NON APPLIQUE")} ({SiteRpOperationalMap.SpawnedCount} elements)\n" +
        $"Audit map automatique: {(SiteRpCorePlugin.AutomaticMapAudit ? "ON" : "OFF")}";

    private static string Arg(ArraySegment<string> arguments, int index) =>
        arguments.Array![arguments.Offset + index];

    private static string JoinFrom(ArraySegment<string> arguments, int index)
    {
        if (arguments.Array is null || index >= arguments.Count)
            return string.Empty;

        return string.Join(" ", arguments.Array, arguments.Offset + index, arguments.Count - index);
    }

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
