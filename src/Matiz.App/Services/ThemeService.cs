using System.Windows;
using System.Windows.Interop;
using Matiz.App.Interop;
using Matiz.Core.Settings;
using Microsoft.Win32;

namespace Matiz.App.Services;

/// <summary>
/// Temas claro/oscuro neutros. Sustituye el diccionario de tokens de color en caliente y
/// sigue el tema de Windows cuando la preferencia es "Sistema".
/// </summary>
public sealed class ThemeService
{
    private ThemePreference _preference = ThemePreference.System;
    private ResourceDictionary? _current;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && _preference == ThemePreference.System)
                Application.Current?.Dispatcher.BeginInvoke(() => Apply(_preference));
        };
    }

    public bool IsDark { get; private set; }

    public event EventHandler? Changed;

    public void Apply(ThemePreference preference)
    {
        _preference = preference;
        IsDark = preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => SystemUsesDarkTheme(),
        };

        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Matiz;component/Themes/{(IsDark ? "Dark" : "Light")}.xaml"),
        };
        var merged = Application.Current.Resources.MergedDictionaries;
        if (_current is not null) merged.Remove(_current);
        else if (merged.Count > 0 && merged[0].Source?.OriginalString is { } src && (src.EndsWith("Dark.xaml") || src.EndsWith("Light.xaml")))
            merged.RemoveAt(0); // tema por defecto declarado en App.xaml
        merged.Insert(0, dict);
        _current = dict;

        foreach (Window w in Application.Current.Windows) ApplyTitleBar(w);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero) NativeMethods.SetDarkTitleBar(hwnd, IsDark);
    }

    private static bool SystemUsesDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return false;
        }
    }
}
