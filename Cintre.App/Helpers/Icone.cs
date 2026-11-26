using System.Windows;
using System.Windows.Media;

namespace Cintre.App.Helpers;

// Propriété attachée : helpers:Icone.Geometrie="{StaticResource IconClients}"
public static class Icone
{
    public static readonly DependencyProperty GeometrieProperty =
        DependencyProperty.RegisterAttached("Geometrie", typeof(Geometry), typeof(Icone),
            new FrameworkPropertyMetadata(null));

    public static Geometry? GetGeometrie(DependencyObject o) => (Geometry?)o.GetValue(GeometrieProperty);

    public static void SetGeometrie(DependencyObject o, Geometry? valeur) => o.SetValue(GeometrieProperty, valeur);
}