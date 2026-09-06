using System.Reflection;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Renders the SiteRP overlay through HintServiceMeow when it is installed.
/// The bridge uses reflection so SiteRP.Core keeps booting even if HSM is absent.
/// Vanilla SendHint is only used as a fallback.
/// </summary>
public static class SiteRpHudRenderer
{
    private static readonly Dictionary<string, object> ActiveHints = new(StringComparer.OrdinalIgnoreCase);

    private static Type? _hintType;
    private static Type? _playerDisplayType;
    private static MethodInfo? _getDisplay;
    private static MethodInfo? _addHint;
    private static MethodInfo? _removeHint;
    private static PropertyInfo? _textProperty;
    private static PropertyInfo? _yCoordinateProperty;
    private static bool _readyLogged;
    private static DateTime _nextResolveAttemptUtc;

    public static void Show(Player player, string text, float fallbackDuration = 30f)
    {
        if (player is null || !player.IsReady)
            return;

        if (TryShowWithHsm(player, text))
            return;

        player.SendHint(text, fallbackDuration);
    }

    public static void Hide(Player player)
    {
        if (player is null)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        if (!ActiveHints.TryGetValue(id, out object? hint))
            return;

        try
        {
            if (EnsureHsm() && _getDisplay is not null && _removeHint is not null)
            {
                object? display = _getDisplay.Invoke(null, new object[] { player });
                if (display is not null)
                    InvokeHintMethod(_removeHint, display, hint);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SiteRP HUD] Impossible de retirer le hint HSM: {ex.GetBaseException().Message}");
        }
        finally
        {
            ActiveHints.Remove(id);
        }
    }

    public static void Cleanup(Player player) => Hide(player);

    private static bool TryShowWithHsm(Player player, string text)
    {
        if (!EnsureHsm() || _hintType is null || _getDisplay is null || _addHint is null || _textProperty is null)
            return false;

        try
        {
            string id = JobRuntime.GetPersistentUserId(player);
            object? display = _getDisplay.Invoke(null, new object[] { player });
            if (display is null)
                return false;

            if (!ActiveHints.TryGetValue(id, out object? hint))
            {
                hint = Activator.CreateInstance(_hintType);
                if (hint is null)
                    return false;

                // HSM uses a 0-1000 vertical coordinate system. Keep SiteRP near the
                // visual centre instead of relying on vanilla <voffset> semantics.
                if (_yCoordinateProperty?.CanWrite == true)
                {
                    Type target = Nullable.GetUnderlyingType(_yCoordinateProperty.PropertyType) ?? _yCoordinateProperty.PropertyType;
                    object y = Convert.ChangeType(470f, target);
                    _yCoordinateProperty.SetValue(hint, y);
                }

                _textProperty.SetValue(hint, text);
                InvokeHintMethod(_addHint, display, hint);
                ActiveHints[id] = hint;
            }
            else
            {
                _textProperty.SetValue(hint, text);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SiteRP HUD] HSM render failed, fallback vanilla: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static void InvokeHintMethod(MethodInfo method, object display, object hint)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Type parameterType = parameters[0].ParameterType;

        if (parameterType.IsArray)
        {
            Type elementType = parameterType.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(hint, 0);
            method.Invoke(display, new object[] { array });
            return;
        }

        method.Invoke(display, new[] { hint });
    }

    private static MethodInfo? FindHintMethod(string name)
    {
        if (_playerDisplayType is null || _hintType is null)
            return null;

        MethodInfo[] methods = _playerDisplayType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == name && m.GetParameters().Length == 1)
            .ToArray();

        MethodInfo? direct = methods.FirstOrDefault(m =>
        {
            Type p = m.GetParameters()[0].ParameterType;
            return !p.IsArray && p.IsAssignableFrom(_hintType);
        });
        if (direct is not null)
            return direct;

        return methods.FirstOrDefault(m =>
        {
            Type p = m.GetParameters()[0].ParameterType;
            Type? element = p.IsArray ? p.GetElementType() : null;
            return element is not null && element.IsAssignableFrom(_hintType);
        });
    }

    private static bool EnsureHsm()
    {
        if (_hintType is not null && _playerDisplayType is not null && _getDisplay is not null && _addHint is not null && _textProperty is not null)
            return true;

        if (DateTime.UtcNow < _nextResolveAttemptUtc)
            return false;

        _nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(2);

        try
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                _hintType ??= assembly.GetType("HintServiceMeow.Core.Models.Hints.Hint", false);
                _playerDisplayType ??= assembly.GetType("HintServiceMeow.Core.Utilities.PlayerDisplay", false);
            }

            if (_hintType is null || _playerDisplayType is null)
                return false;

            _textProperty = _hintType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
            _yCoordinateProperty = _hintType.GetProperty("YCoordinate", BindingFlags.Public | BindingFlags.Instance);

            _getDisplay = _playerDisplayType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "Get")
                        return false;
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 1 && p[0].ParameterType.IsAssignableFrom(typeof(Player));
                });

            _addHint = FindHintMethod("AddHint");
            _removeHint = FindHintMethod("RemoveHint");

            bool ready = _textProperty is not null && _getDisplay is not null && _addHint is not null;
            if (ready && !_readyLogged)
            {
                _readyLogged = true;
                Logger.Info("[SiteRP HUD] HintServiceMeow detected: persistent SiteRP overlay renderer active.");
            }

            return ready;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SiteRP HUD] Detection HSM impossible: {ex.GetBaseException().Message}");
            return false;
        }
    }
}
