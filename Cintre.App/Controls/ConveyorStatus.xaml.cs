using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Cintre.App.Models;

namespace Cintre.App.Controls;

// Convoyeur horizontal : le vêtement glisse jusqu'à l'étape du statut (Reçu, Traitement, Prêt, Retiré).
// Un statut Annulé garde le vêtement à la première étape, en rouge.
// Usage : <controls:ConveyorStatus Statut="{Binding Statut}"/>
public partial class ConveyorStatus : UserControl
{
    private const double TailleVetement = 34;
    private static readonly TimeSpan Duree = TimeSpan.FromMilliseconds(600);

    public static readonly DependencyProperty StatutProperty = DependencyProperty.Register(
        nameof(Statut), typeof(StatutCommande), typeof(ConveyorStatus),
        new PropertyMetadata(StatutCommande.Recu, (d, e) => ((ConveyorStatus)d).Actualiser(true)));

    public StatutCommande Statut
    {
        get => (StatutCommande)GetValue(StatutProperty);
        set => SetValue(StatutProperty, value);
    }

    private readonly Ellipse[] _points;
    private readonly TextBlock[] _etapes;

    public ConveyorStatus()
    {
        InitializeComponent();
        _points = new[] { Point0, Point1, Point2, Point3 };
        _etapes = new[] { Etape0, Etape1, Etape2, Etape3 };

        // À l'affichage, le vêtement entre depuis la gauche jusqu'à son étape
        Loaded += (_, _) =>
        {
            Deplacement.X = 0;
            Progression.Width = 0;
            Actualiser(true);
        };
    }

    private void Racine_SizeChanged(object sender, SizeChangedEventArgs e) => Actualiser(false);

    private static int Index(StatutCommande s) => s switch
    {
        StatutCommande.EnTraitement => 1,
        StatutCommande.Pret => 2,
        StatutCommande.Retire => 3,
        _ => 0 // Recu et Annule
    };

    private static Brush Couleur(StatutCommande s)
    {
        var cle = s switch
        {
            StatutCommande.EnTraitement => "StatutTraitementBrush",
            StatutCommande.Pret => "StatutPretBrush",
            StatutCommande.Retire => "StatutRetireBrush",
            StatutCommande.Annule => "StatutAnnuleBrush",
            _ => "StatutRecuBrush"
        };
        return (Brush)Application.Current.FindResource(cle);
    }

    private void Actualiser(bool animer)
    {
        double largeur = Racine.ActualWidth;
        if (largeur <= 0) return; // pas encore affiché : Loaded s'en chargera

        int index = Index(Statut);
        double centre = (index + 0.5) * largeur / 4;
        double cible = centre - TailleVetement / 2;
        var couleur = Couleur(Statut);

        Vetement.Background = couleur;
        Progression.Background = couleur;
        Etape0.Text = Statut == StatutCommande.Annule ? "Annulée" : "Reçu";

        var discret = (Brush)Application.Current.FindResource("TexteDiscretBrush");
        var contour = (Brush)Application.Current.FindResource("SeparateurBrush");
        for (int i = 0; i < 4; i++)
        {
            bool atteint = i <= index;
            _points[i].Fill = atteint ? couleur : Brushes.White;
            _points[i].Stroke = atteint ? couleur : contour;
            _etapes[i].Foreground = i == index ? couleur : discret;
            _etapes[i].FontWeight = i == index ? FontWeights.SemiBold : FontWeights.Normal;
        }

        if (animer)
        {
            var douceur = new CubicEase { EasingMode = EasingMode.EaseInOut };
            Deplacement.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(cible, Duree) { EasingFunction = douceur });
            Progression.BeginAnimation(WidthProperty,
                new DoubleAnimation(centre, Duree) { EasingFunction = douceur });
        }
        else
        {
            Deplacement.BeginAnimation(TranslateTransform.XProperty, null);
            Deplacement.X = cible;
            Progression.BeginAnimation(WidthProperty, null);
            Progression.Width = centre;
        }
    }
}