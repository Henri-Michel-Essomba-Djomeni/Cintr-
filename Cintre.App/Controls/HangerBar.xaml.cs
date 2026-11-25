using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cintre.App.Controls;

// Barre de 5 à 7 cintres avec vêtements qui se balancent doucement, chacun à son rythme.
// Usage : <controls:HangerBar Nombre="6"/>   (Couleur = celle de la tringle et des cintres)
public partial class HangerBar : UserControl
{
    private static readonly string[] CouleursVetements =
    {
        "PrimaireBrush", "AccentBrush", "StatutRecuBrush",
        "SuccesBrush", "StatutTraitementBrush", "PrimaireFonceBrush"
    };

    public static readonly DependencyProperty NombreProperty = DependencyProperty.Register(
        nameof(Nombre), typeof(int), typeof(HangerBar), new PropertyMetadata(6));

    public static readonly DependencyProperty CouleurProperty = DependencyProperty.Register(
        nameof(Couleur), typeof(Brush), typeof(HangerBar), new PropertyMetadata(null));

    // Entre 5 et 7
    public int Nombre
    {
        get => (int)GetValue(NombreProperty);
        set => SetValue(NombreProperty, value);
    }

    public Brush? Couleur
    {
        get => (Brush?)GetValue(CouleurProperty);
        set => SetValue(CouleurProperty, value);
    }

    public HangerBar()
    {
        InitializeComponent();
        Loaded += (_, _) => Construire();
    }

    private void Construire()
    {
        var trait = Couleur ?? (Brush)FindResource("TexteDiscretBrush");
        Tringle.Background = trait;

        Cintres.Children.Clear();
        int n = Math.Clamp(Nombre, 5, 7);
        Cintres.Columns = n;

        for (int i = 0; i < n; i++)
            Cintres.Children.Add(CreerCintre(i, trait));
    }

    private FrameworkElement CreerCintre(int i, Brush trait)
    {
        // Le vêtement (robe), derrière le cintre
        var vetement = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M5,17 L19,17 L22,38 L2,38 Z"),
            Fill = (Brush)FindResource(CouleursVetements[i % CouleursVetements.Length]),
            Opacity = 0.92,
            Stroke = Brushes.Transparent,
            StrokeThickness = 1,
            StrokeLineJoin = PenLineJoin.Round
        };

        // Le cintre (même dessin que l'icône du logo), par-dessus
        var cintre = new System.Windows.Shapes.Path
        {
            Data = (Geometry)FindResource("IconCintre"),
            Stroke = trait,
            StrokeThickness = 1.6,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        var dessin = new Canvas { Width = 24, Height = 40 };
        dessin.Children.Add(vetement);
        dessin.Children.Add(cintre);

        // Le pivot est en haut, au niveau du crochet
        var rotation = new RotateTransform(-7);
        var conteneur = new Viewbox
        {
            Height = 58,
            Child = dessin,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new Point(0.5, 0),
            RenderTransform = rotation
        };

        // Balancement avec ralentissement aux extrémités ; chaque cintre a sa durée et son décalage
        var balancement = new DoubleAnimation(-7, 7, TimeSpan.FromSeconds(1.4 + 0.25 * (i % 4)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromMilliseconds(i * 180),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        rotation.BeginAnimation(RotateTransform.AngleProperty, balancement);

        return conteneur;
    }
}