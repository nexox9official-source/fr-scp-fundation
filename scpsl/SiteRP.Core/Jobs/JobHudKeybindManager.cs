namespace SiteRP.Core.Jobs;

/// <summary>
/// Compatibility stub. SiteRP no longer injects keybinds into Server-Specific Settings,
/// because M is reserved for the native/admin interface on this server.
/// Players choose their own keys with SCP:SL bind/cmdbind and the .hud command.
/// </summary>
public static class JobHudKeybindManager
{
    public static void Register()
    {
        Logger.Info("[SiteRP HUD] Server-specific keybind injection disabled. Players use bind/cmdbind with .hud; M remains untouched.");
    }

    public static void Unregister()
    {
    }
}
