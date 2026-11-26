using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cintre.App.Controls;

// Fond animé de Lavea : des vêtements en emoji montent lentement, se balancent et tournent.
// Usage : placer <controls:FondAnime/> en premier dans une Grid, puis le contenu par-dessus.
public partial class FondAnime : UserControl
{
    private static readonly string[] Emojis =
        { "👕", "👖", "👗", "🧥", "🧦", "🧺", "👔", "🧣", "🧼", "👚", "🩳", "🧤", "👕", "👗" };

    private readonly List<(TranslateTransform Deplacement, RotateTransform Rotation)> _animes = new();
    private double _largeur;
    private double _hauteur;

    public FondAnime()
    {
        InitializeComponent();
        Loaded += (_, _) => Demarrer();
        Unloaded += (_, _) => Arreter();
        SizeChanged += (_, e) =>
        {
            // On relance seulement si la fenêtre a beaucoup changé de taille
            if (IsLoaded && (Math.Abs(e.NewSize.Width - _largeur) > 250 || Math.Abs(e.NewSize.Height - _hauteur) > 250))
                Demarrer();
        };
    }

    private void Demarrer()
    {
        Arreter();
        _largeur = ActualWidth > 0 ? ActualWidth : 1000;
        _hauteur = ActualHeight > 0 ? ActualHeight : 700;

        var hasard = new Random(7); // même disposition à chaque ouverture
        // Si Windows désactive les animations, les vêtements restent immobiles
        var animer = SystemParameters.ClientAreaAnimation;

        for (var i = 0; i < Emojis.Length; i++)
        {
            var taille = hasard.Next(26, 50);
            var duree = hasard.Next(18, 34);

            var rotation = new RotateTransform(0);
            var deplacement = new TranslateTransform(0, 0);
            var groupe = new TransformGroup();
            groupe.Children.Add(rotation);
            groupe.Children.Add(deplacement);

            var texte = new TextBlock
            {
                Text = Emojis[i],
                FontFamily = new FontFamily("Segoe UI Emoji"),
                FontSize = taille,
                Opacity = 0.22,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = groupe
            };
            Canvas.SetLeft(texte, (i + 0.5) / Emojis.Length * _largeur - taille / 2.0);
            Scene.Children.Add(texte);

            if (!animer)
            {
                Canvas.SetTop(texte, hasard.NextDouble() * _hauteur);
                continue;
            }

            Canvas.SetTop(texte, _hauteur); // départ sous le bord bas, caché par le clip

            // Montée continue ; un début négatif fait démarrer chaque vêtement à un endroit différent
            deplacement.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, -(_hauteur + 120), TimeSpan.FromSeconds(duree))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromSeconds(-hasard.Next(0, duree))
                });

            // Balancement de côté
            deplacement.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(-28, 28, TimeSpan.FromSeconds(duree / 3.0))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase()
                });

            // Rotation douce
            rotation.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(-20, 20, TimeSpan.FromSeconds(duree / 2.0))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase()
                });

            _animes.Add((deplacement, rotation));
        }
    }

    // Arrête les animations quand l'écran est quitté (rien ne tourne en arrière-plan)
    private void Arreter()
    {
        foreach (var (deplacement, rotation) in _animes)
        {
            deplacement.BeginAnimation(TranslateTransform.YProperty, null);
            deplacement.BeginAnimation(TranslateTransform.XProperty, null);
            rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }
        _animes.Clear();
        Scene.Children.Clear();
    }
}
