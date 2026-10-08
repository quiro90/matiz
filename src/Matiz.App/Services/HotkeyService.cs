using System.Windows.Input;
using System.Windows.Interop;
using static Matiz.App.Interop.NativeMethods;

namespace Matiz.App.Services;

/// <summary>Atajo global (RegisterHotKey) sobre una ventana de solo mensajes.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x4D5A; // "MZ"
    private readonly HwndSource _source;
    private bool _registered;

    public HotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("MatizHotkey") { ParentWindow = HWND_MESSAGE, WindowStyle = 0 });
        _source.AddHook(WndProc);
    }

    public event EventHandler? Pressed;

    public string? Current { get; private set; }

    /// <summary>Registra el atajo (p. ej. "Alt+C"). Devuelve false si no se puede (formato inválido u ocupado).</summary>
    public bool Register(string gesture)
    {
        Unregister();
        if (!HotkeyGesture.TryParse(gesture, out var mods, out var key)) return false;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        uint m = MOD_NOREPEAT;
        if (mods.HasFlag(ModifierKeys.Alt)) m |= MOD_ALT;
        if (mods.HasFlag(ModifierKeys.Control)) m |= MOD_CONTROL;
        if (mods.HasFlag(ModifierKeys.Shift)) m |= MOD_SHIFT;
        if (mods.HasFlag(ModifierKeys.Windows)) m |= MOD_WIN;
        _registered = RegisterHotKey(_source.Handle, HotkeyId, m, vk);
        Current = _registered ? HotkeyGesture.Format(mods, key) : null;
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered) return;
        UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
        Current = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.Dispose();
    }
}

/// <summary>Texto ↔ combinación de teclas ("Ctrl+Alt+C", "Win+Shift+C").</summary>
public static class HotkeyGesture
{
    public static bool TryParse(string? text, out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts[..^1])
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl": case "control": modifiers |= ModifierKeys.Control; break;
                case "alt": modifiers |= ModifierKeys.Alt; break;
                case "shift": modifiers |= ModifierKeys.Shift; break;
                case "win": case "windows": modifiers |= ModifierKeys.Windows; break;
                default: return false;
            }
        }
        if (!Enum.TryParse(parts[^1], ignoreCase: true, out key) || key == Key.None) return false;
        return modifiers != ModifierKeys.None;
    }

    public static string Format(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
