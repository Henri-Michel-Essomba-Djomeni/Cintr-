using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Cintre.App.Models;

namespace Cintre.App.Controls;

// Graphique en barres dessiné avec des Rectangle WPF qui poussent depuis le bas (aucune bibliothèque).
// Usage : <controls:GraphiqueBarres Donnees="{Binding Recettes}" MessageVide="Aucune recette"/>
public class GraphiqueBarres : Grid
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly Brush Encre = Figer("#0B3C49");
    private static readonly Brush Discret = Figer("#4A6772");

    public static readonly DependencyProperty DonneesProperty = DependencyProperty.Register(
        nameof(Donnees), typeof(IEnumerable<PointStat>), typeof(GraphiqueBarres),
        new PropertyMetadata(null, (d, _) => ((GraphiqueBarres)d).Reconstruire()));

    public static readonly DependencyProperty MessageVideProperty = DependencyProperty.Register(
        nameof(MessageVide), typeof(string), typeof(GraphiqueBarres),
        new PropertyMetadata("Aucune donnée sur cette période.", (d, _) => ((GraphiqueBarres)d).Reconstruire()));

    public static readonly DependencyProperty CouleurBarreProperty = DependencyProperty.Register(
        nameof(CouleurBarre), typeof(Brush), typeof(GraphiqueBarres),
        new PropertyMetadata(Figer("#0B4F60"), (d, _) => ((GraphiqueBarres)d).Reconstruire()));

    public IEnumerable<PointStat>? Donnees
    {
        get => (IEnumerable<PointStat>?)GetValue(DonneesProperty);
        set => SetValue(DonneesProperty, value);
    }

    public string MessageVide
    {
        get => (string)GetValue(MessageVideProperty);
        set => SetValue(MessageVideProperty, value);
    }

    public Brush CouleurBarre
    {
        get => (Brush)GetValue(CouleurBarreProperty);
        set => SetValue(CouleurBarreProperty, value);
    }

    public GraphiqueBarres()
    {
        // À chaque affichage de l'écran, les barres repoussent depuis le bas
        Loaded += (_, _) => Reconstruire();
    }

    private void Reconstruire()
    {
        Children.Clear();
        var points = Donnees?.ToList() ?? new List<PointStat>();
        var max = points.Count == 0 ? 0m : points.Max(p => p.Valeur);

        // Pas de données ou que des zéros : un message, jamais de division par zéro
        if (points.Count == 0 || max <= 0)
        {
            Children.Add(new TextBlock
            {
                Text = MessageVide,
                FontSize = 14,
                Foreground = Discret,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            return;
        }

        var colonnes = new UniformGrid { Rows = 1, Columns = points.Count };
        for (var i = 0; i < points.Count; i++)
            colonnes.Children.Add(CreerColonne(points[i], i, points.Count, (double)(points[i].Valeur / max)));
        Children.Add(colonnes);
    }

    private FrameworkElement CreerColonne(PointStat p, int index, int total, double ratio)
    {
        var cellule = new Grid { Margin = new Thickness(total <= 10 ? 8 : 3, 0, total <= 10 ? 8 : 3, 0) };
        cellule.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Une barre positive reste toujours visible, même minuscule
        if (p.Valeur > 0) ratio = Math.Max(ratio, 0.02);
        cellule.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - ratio, GridUnitType.Star) });
        cellule.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ratio, GridUnitType.Star) });
        cellule.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Valeur au-dessus de la barre (seulement quand il y a peu de barres)
        if (total <= 12)
        {
            var valeur = new TextBlock
            {
                Text = p.Valeur.ToString("N0", Fr),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Encre,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(valeur, 0);
            cellule.Children.Add(valeur);
        }

        // La barre : un Rectangle qui pousse depuis le bas
        var echelle = new ScaleTransform(1, 0);
        var barre = new Rectangle
        {
            Fill = CouleurBarre,
            RadiusX = 6,
            RadiusY = 6,
            RenderTransformOrigin = new Point(0.5, 1),
            RenderTransform = echelle,
            ToolTip = $"{p.Libelle} : {p.Valeur.ToString("N0", Fr)} FCFA"
        };
        Grid.SetRow(barre, 2);
        cellule.Children.Add(barre);

        echelle.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(650))
            {
                BeginTime = TimeSpan.FromMilliseconds(Math.Min(index * 40, 800)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        // Date en dessous : "25/11" s'il y a peu de barres, sinon seulement le jour, une sur deux ou trois
        var pas = total <= 31 ? 1 : total <= 62 ? 2 : 3;
        if (total <= 10 || index % pas == 0)
        {
            var libelle = total <= 10 || p.Libelle.Length < 2 ? p.Libelle : p.Libelle[..2];
            var date = new TextBlock
            {
                Text = libelle,
                FontSize = total <= 10 ? 12 : 10,
                Foreground = Discret,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };
            Grid.SetRow(date, 3);
            cellule.Children.Add(date);
        }

        return cellule;
    }

    private static Brush Figer(string hex)
    {
        var pinceau = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        pinceau.Freeze();
        return pinceau;
    }
}
