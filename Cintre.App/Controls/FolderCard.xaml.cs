using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Cintre.App.Controls;

// Classeur de client : la couverture se rabat au survol et révèle le résumé du client.
// Usage : <controls:FolderCard Nom="{Binding Nom}" Telephone="{Binding Telephone}"
//                              Points="{Binding PointsFidelite}" NbCommandes="{Binding NbCommandes}"/>
public partial class FolderCard : UserControl
{
    public static readonly DependencyProperty NomProperty = DependencyProperty.Register(
        nameof(Nom), typeof(string), typeof(FolderCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TelephoneProperty = DependencyProperty.Register(
        nameof(Telephone), typeof(string), typeof(FolderCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(int), typeof(FolderCard), new PropertyMetadata(0));

    public static readonly DependencyProperty NbCommandesProperty = DependencyProperty.Register(
        nameof(NbCommandes), typeof(int), typeof(FolderCard), new PropertyMetadata(0));

    public string Nom
    {
        get => (string)GetValue(NomProperty);
        set => SetValue(NomProperty, value);
    }

    public string Telephone
    {
        get => (string)GetValue(TelephoneProperty);
        set => SetValue(TelephoneProperty, value);
    }

    public int Points
    {
        get => (int)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public int NbCommandes
    {
        get => (int)GetValue(NbCommandesProperty);
        set => SetValue(NbCommandesProperty, value);
    }

    public FolderCard()
    {
        InitializeComponent();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        Ouvrir(true);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        Ouvrir(false);
    }

    private void Ouvrir(bool ouvert)
    {
        var douceur = new CubicEase { EasingMode = EasingMode.EaseOut };

        EchelleCouverture.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(ouvert ? 0.3 : 1, TimeSpan.FromMilliseconds(350)) { EasingFunction = douceur });

        ContenuCouverture.BeginAnimation(OpacityProperty,
            new DoubleAnimation(ouvert ? 0 : 1, TimeSpan.FromMilliseconds(200)));

        // La carte se soulève légèrement (carte cliquable)
        Soulevement.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(ouvert ? -4 : 0, TimeSpan.FromMilliseconds(250)) { EasingFunction = douceur });
    }
}