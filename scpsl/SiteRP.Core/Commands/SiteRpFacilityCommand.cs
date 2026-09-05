using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SiteRpFacilityCommand : ICommand
{
    public string Command => "siterpfacility";
    public string[] Aliases => new[] { "srpfacility", "facilityrp" };
    public string Description => "Plan et audit de la SiteRP Operational Facility inspiree de Site-76.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is not null && !player.RemoteAdminAccess)
        {
            response = "Acces refuse : Remote Admin requis.";
            return false;
        }

        string action = arguments.Count > 0 ? Arg(arguments, 0).ToLowerInvariant() : "plan";
        switch (action)
        {
            case "plan":
            case "status":
                response = SiteRpFacilityPlan.Describe();
                return true;

            case "audit":
            case "scan":
                response = "Facility audit genere: " + SiteRpFacilitySurvey.WriteAudit();
                return true;

            case "report":
            case "last":
                response = string.IsNullOrEmpty(SiteRpFacilitySurvey.LastReportPath)
                    ? "Aucun Facility Audit dans cette session. Utilise: siterpfacility audit"
                    : "Dernier Facility Audit: " + SiteRpFacilitySurvey.LastReportPath;
                return true;

            default:
                response = "Usage: siterpfacility plan | audit | report";
                return false;
        }
    }

    private static string Arg(ArraySegment<string> arguments, int index) =>
        arguments.Array![arguments.Offset + index];
}
