using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SiteRpScpCommand : ICommand
{
    public string Command => "siterpscp";
    public string[] Aliases => new[] { "srpscp", "scpstate" };
    public string Description => "Controle les etats persistants SCP et C.A.S.S.I.E./079 de SiteRP.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is not null && !player.RemoteAdminAccess)
        {
            response = "Acces refuse : Remote Admin requis.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = SiteRpScpStateManager.Status();
            return true;
        }

        string action = Arg(arguments, 0).ToLowerInvariant();
        switch (action)
        {
            case "status":
            case "list":
                response = SiteRpScpStateManager.Status();
                return true;

            case "site":
                if (arguments.Count < 2)
                {
                    response = "Usage: siterpscp site NORMAL|INCIDENT|BREACH|MAJOR_BREACH|EVACUATION";
                    return false;
                }
                return SiteRpScpStateManager.TrySetSiteState(Arg(arguments, 1), out response);

            case "set":
                if (arguments.Count < 3)
                {
                    response = "Usage: siterpscp set <scp> <CONTAINED|TESTING|COOPERATIVE|HOSTILE|BREACHED|RECONTAINING|RECONTAINED|DISABLED>";
                    return false;
                }
                return SiteRpScpStateManager.TrySetScpState(Arg(arguments, 1), Arg(arguments, 2), out response);

            case "release":
            case "breach":
                return SetQuick(arguments, "breached", out response);

            case "contain":
            case "contained":
                return SetQuick(arguments, "contained", out response);

            case "test":
            case "testing":
                return SetQuick(arguments, "testing", out response);

            case "coop":
            case "cooperative":
                return SetQuick(arguments, "cooperative", out response);

            case "hostile":
                return SetQuick(arguments, "hostile", out response);

            case "disable":
            case "disabled":
                return SetQuick(arguments, "disabled", out response);

            case "079":
            case "cassie":
                return Handle079(arguments, out response);

            case "reset":
                SiteRpScpStateManager.Reset();
                response = "Etats SiteRP SCP reinitialises: Site NORMAL, SCP confines, C.A.S.S.I.E. cooperative.";
                return true;

            default:
                response = Help();
                return false;
        }
    }

    private static bool Handle079(ArraySegment<string> arguments, out string response)
    {
        if (arguments.Count < 2 || Arg(arguments, 1).Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            response = $"SCP-079 / C.A.S.S.I.E.: {SiteRpScpStateManager.Get("079").ToString().ToUpperInvariant()}\n" +
                       $"Cameras: {(SiteRpScpStateManager.Can079UseCameras ? "ON" : "OFF")} | " +
                       $"Support Fondation: {(SiteRpScpStateManager.Can079UseFoundationSupport ? "ON" : "OFF")} | " +
                       $"Systemes hostiles: {(SiteRpScpStateManager.Can079UseHostileSystems ? "ON" : "OFF")}";
            return true;
        }

        string mode = Arg(arguments, 1).ToLowerInvariant();
        string state = mode switch
        {
            "healthy" or "cassie" or "foundation" or "coop" => "cooperative",
            "test" or "testing" => "testing",
            "compromise" or "compromised" or "breach" or "breached" => "breached",
            "hostile" => "hostile",
            "disable" or "disabled" or "offline" => "disabled",
            "contain" or "contained" => "contained",
            "recontained" => "recontained",
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(state))
        {
            response = "Usage: siterpscp 079 status|cassie|test|compromised|hostile|disabled|contained|recontained";
            return false;
        }

        return SiteRpScpStateManager.TrySetScpState("079", state, out response);
    }

    private static bool SetQuick(ArraySegment<string> arguments, string state, out string response)
    {
        if (arguments.Count < 2)
        {
            response = $"Usage: siterpscp {Arg(arguments, 0)} <scp>";
            return false;
        }
        return SiteRpScpStateManager.TrySetScpState(Arg(arguments, 1), state, out response);
    }

    private static string Help() =>
        "SiteRP SCP commands:\n" +
        "siterpscp status\n" +
        "siterpscp site NORMAL|INCIDENT|BREACH|MAJOR_BREACH|EVACUATION\n" +
        "siterpscp set <scp> <state>\n" +
        "siterpscp release|contain|test|coop|hostile|disable <scp>\n" +
        "siterpscp 079 status|cassie|test|compromised|hostile|disabled|contained|recontained\n" +
        "siterpscp reset";

    private static string Arg(ArraySegment<string> arguments, int index) =>
        arguments.Array![arguments.Offset + index];
}
