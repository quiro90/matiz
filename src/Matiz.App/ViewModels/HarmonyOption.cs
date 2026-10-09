using System.ComponentModel;
using Matiz.Core.Generation;

namespace Matiz.App.ViewModels;

/// <summary>Opción de la lista de armonías (la fila de chips). Nombre mutable: el refresh de idioma lo
/// actualiza en el sitio (INPC) para no reemplazar la lista y así no perder la selección del ListBox.</summary>
public sealed class HarmonyOption : INotifyPropertyChanged
{
    private string _name;

    public HarmonyOption(HarmonyKind value, string name)
    {
        Value = value;
        _name = name;
    }

    public HarmonyKind Value { get; }

    public string Name
    {
        get => _name;
        private set
        {
            if (!string.Equals(_name, value, StringComparison.Ordinal))
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    public void RefreshName() => Name = PaletteGenerator.HarmonyName(Value);

    public event PropertyChangedEventHandler? PropertyChanged;
}