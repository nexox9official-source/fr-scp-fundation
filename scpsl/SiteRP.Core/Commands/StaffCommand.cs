using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class StaffCommand : ICommand
{
    public string Command => "staff";
    public string[] Aliases => new[] { "staffmode", "modstaff" };
    public string Description => "Active ou desactive le mode staff SiteRP.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = "Cette commande doit etre executee par un joueur.";
            return false;
        }

        if (!player.RemoteAdminAccess)
        {
            response = "Acces refuse : Remote Admin requis.";
            return false;
        }

        if (SiteRpCorePlugin.IsStaffMode(player))
            return SiteRpCorePlugin.ExitStaffMode(player, out response);

        return SiteRpCorePlugin.EnterStaffMode(player, out response);
    }
}
