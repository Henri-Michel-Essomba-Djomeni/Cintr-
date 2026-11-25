using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace Cintre.App.Services;

// Notifications qui glissent en haut à droite et disparaissent seules après 3 secondes.
public static class ToastService
{
    private const int DureeAffichageMs = 3000;
    private const int MaxVisibles = 4;

    private static Panel? _hote;

    public static bool EstPret => _hote != null;

    public static void Initialiser(Panel hote) => _hote = hote;

    public static void Succes(string message) => Afficher(message, "SuccesBrush", "IconCoche");

    public static void Erreur(string message) => Afficher(message, "DangerBrush", "IconFermer");

    public static void Info(string message) => Afficher(message, "PrimaireBrush", "IconInfo");

    private static void Afficher(string message, string cleCouleur, string cleIcone)
    {
        var app = Application.Current;
        if (app == null) return;

        // Peut être appelé depuis un autre thread (tâche de fond) : on repasse sur l'interface
        app.Dispatcher.BeginInvoke(() => Creer(message, cleCouleur, cleIcone));
    }

    private static void Creer(string message, string cleCouleur, string cleIcone)
    {
        if (_hote == null) return;

        var couleur = (Brush)Application.Current.FindResource(cleCouleur);
        var icone = (Geometry)Application.Current.FindResource(cleIcone);

        var trace = new System.Windows.Shapes.Path
        {
            Data = icone,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        var canvas = new Canvas { Width = 24, Height = 24 };
        canvas.Children.Add(trace);
        var vueIcone = new Viewbox
        {
            Width = 20,
            Height = 20,
            Child = canvas,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var texte = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(texte, 1);

        var contenu = new Grid();
        contenu.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contenu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contenu.Children.Add(vueIcone);
        contenu.Children.Add(texte);

        var deplacement = new TranslateTransform(40, 0);
        var carte = new Border
        {
            Background = couleur,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 10),
            Child = contenu,
            Opacity = 0,
            RenderTransform = deplacement,
            Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 3, Opacity = 0.25, Color = Colors.Black }
        };

        // Pas plus de 4 toasts à l'écran : le plus ancien part
        while (_hote.Children.Count >= MaxVisibles)
            _hote.Children.RemoveAt(0);

        _hote.Children.Add(carte);

        var entree = TimeSpan.FromMilliseconds(250);
        deplacement.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(40, 0, entree) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        carte.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, entree));

        var minuteur = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DureeAffichageMs) };
        minuteur.Tick += (_, _) =>
        {
            minuteur.Stop();
            Disparaitre(carte);
        };
        minuteur.Start();
    }

    private static void Disparaitre(Border carte)
    {
        var fondu = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        fondu.Completed += (_, _) => _hote?.Children.Remove(carte);
        carte.BeginAnimation(UIElement.OpacityProperty, fondu);
    }
}