using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Matiz.App.Controls;

/// <summary>UniformGrid con ancho mínimo por celda: si el ancho disponible es menor que
/// celdas × MinCellWidth, el panel crece en vez de comprimir los ítems (con ScrollViewer aparece la barra).</summary>
public sealed class MinCellUniformGrid : UniformGrid
{
    public static readonly DependencyProperty MinCellWidthProperty = DependencyProperty.Register(
        nameof(MinCellWidth), typeof(double), typeof(MinCellUniformGrid),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinCellWidth
    {
        get => (double)GetValue(MinCellWidthProperty);
        set => SetValue(MinCellWidthProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var desired = base.MeasureOverride(constraint);
        var minTotalWidth = MinCellWidth * EffectiveColumns;
        return minTotalWidth > desired.Width ? new Size(minTotalWidth, desired.Height) : desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(finalSize.Width, MinCellWidth * EffectiveColumns);
        return base.ArrangeOverride(new Size(width, finalSize.Height));
    }

    private int EffectiveColumns
    {
        get
        {
            if (Columns > 0) return Columns;
            var count = Math.Max(InternalChildren.Count, 1);
            if (Rows > 0) return (int)Math.Ceiling(count / (double)Rows);
            return (int)Math.Ceiling(Math.Sqrt(count));
        }
    }
}