using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Cintre.App.Models;

namespace Cintre.App.Controls;

// Classement en lignes : un libellé, une valeur et une barre proportionnelle qui se remplit.
// Usage : <controls:ListeClassement Donnees="{Binding TopClients}" Suffixe=" FCFA"/>
public class ListeClassement : StackPanel
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly Brush Encre = Figer("#0B3C49");
    private static readonly Brush Discret = Figer("#4A6772");
    private static readonly Brush Piste = Figer("#E8DDB8");

    public static readonly DependencyProperty DonneesProperty = DependencyProperty.Register(
        nameof(Donnees), typeof(IEnumerable<PointStat>), typeof(ListeClassement),
        new PropertyMetadata(null, (d, _) => ((ListeClassement)d).Reconstruire()));

    public static readonly DependencyProperty SuffixeProperty = DependencyProperty.Register(
        nameof(Suffixe), typeof(string), typeof(ListeClassement),
        new PropertyMetadata("", (d, _) => ((ListeClassement)d).Reconstruire()));

    public static readonly DependencyProperty MessageVideProperty = DependencyProperty.Register(
        nameof(MessageVide), typeof(string), typeof(ListeClassement),
        new PropertyMetadata("Aucune donnée pour le moment.", (d, _) => ((ListeClassement)d).Reconstruire()));

    public static readonly DependencyProperty CouleurBarreProperty = DependencyProperty.Register(
        nameof(CouleurBarre), typeof(Brush), typeof(ListeClassement),
        new PropertyMetadata(Figer("#0B4F60"), (d, _) => ((ListeClassement)d).Reconstruire()));

    public IEnumerable<PointStat>? Donnees
    {
        get => (IEnumerable<PointStat>?)GetValue(DonneesProperty);
        set => SetValue(DonneesProperty, value);
    }

    public string Suffixe
    {
        get => (string)GetValue(SuffixeProperty);
        set => SetValue(SuffixeProperty, value);
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

    public ListeClassement()
    {
        Loaded += (_, _) => Reconstruire();
    }

    private void Reconstruire()
    {
        Children.Clear();
        var points = Donnees?.ToList() ?? new List<PointStat>();
        if (points.Count == 0)
        {
            Children.Add(new TextBlock { Text = MessageVide, FontSize = 13, Foreground = Discret, TextWrapping = TextWrapping.Wrap });
            return;
        }

        var max = points.Max(p => p.Valeur);
        for (var i = 0; i < points.Count; i++)
        {
            // Si tout vaut zéro, les barres restent vides : pas de division par zéro
            var ratio = max > 0 ? (double)(points[i].Valeur / max) : 0;
            Children.Add(CreerLigne(points[i], i, ratio));
        }
    }

    private FrameworkElement CreerLigne(PointStat p, int index, double ratio)
    {
        var ligne = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        ligne.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ligne.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Libellé à gauche, valeur à droite
        var haut = new Grid();
        haut.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        haut.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nom = new TextBlock
        {
            Text = p.Libelle,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Encre,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 10, 0)
        };
        var valeur = new TextBlock
        {
            Text = p.Valeur.ToString("N0", Fr) + Suffixe,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Discret
        };
        Grid.SetColumn(valeur, 1);
        haut.Children.Add(nom);
        haut.Children.Add(valeur);
        Grid.SetRow(haut, 0);
        ligne.Children.Add(haut);

        // La piste, et la barre qui se remplit de gauche à droite
        var piste = new Grid { Height = 8, Margin = new Thickness(0, 6, 0, 0) };
        var fondPiste = new Border { Background = Piste, CornerRadius = new CornerRadius(4) };
        piste.Children.Add(fondPiste);

        var remplissage = new Grid();
        remplissage.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ratio, GridUnitType.Star) });
        remplissage.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - ratio, GridUnitType.Star) });

        var echelle = new ScaleTransform(0, 1);
        var barre = new Border
        {
            Background = CouleurBarre,
            CornerRadius = new CornerRadius(4),
            RenderTransformOrigin = new Point(0, 0.5),
            RenderTransform = echelle
        };
        remplissage.Children.Add(barre);
        piste.Children.Add(remplissage);
        Grid.SetRow(piste, 1);
        ligne.Children.Add(piste);

        echelle.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600))
            {
                BeginTime = TimeSpan.FromMilliseconds(index * 90),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        return ligne;
    }

    private static Brush Figer(string hex)
    {
        var pinceau = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        pinceau.Freeze();
        return pinceau;
    }
}
