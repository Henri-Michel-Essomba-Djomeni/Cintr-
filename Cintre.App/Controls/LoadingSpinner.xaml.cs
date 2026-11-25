using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cintre.App.Controls;

// Indicateur de chargement : <controls:LoadingSpinner EstActif="{Binding EstEnChargement}"/>
// Invisible (et sans animation) tant que EstActif vaut false.
public partial class LoadingSpinner : UserControl
{
    public static readonly DependencyProperty EstActifProperty = DependencyProperty.Register(
        nameof(EstActif), typeof(bool), typeof(LoadingSpinner),
        new PropertyMetadata(false, (d, e) => ((LoadingSpinner)d).Appliquer((bool)e.NewValue)));

    public bool EstActif
    {
        get => (bool)GetValue(EstActifProperty);
        set => SetValue(EstActifProperty, value);
    }

    public LoadingSpinner()
    {
        InitializeComponent();
        Appliquer(EstActif);
    }

    private void Appliquer(bool actif)
    {
        Visibility = actif ? Visibility.Visible : Visibility.Collapsed;

        if (actif)
        {
            Rotation.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.9)) { RepeatBehavior = RepeatBehavior.Forever });
        }
        else
        {
            Rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}