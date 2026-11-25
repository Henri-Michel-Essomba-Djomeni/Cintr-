using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Cintre.App.Controls;

// Nombre qui s'incrémente de 0 à sa valeur en 1 seconde.
// Usage : <controls:CountUpText Valeur="{Binding RecetteJour}" Suffixe=" FCFA" FontSize="28"/>
public class CountUpText : TextBlock
{
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    public static readonly DependencyProperty ValeurProperty = DependencyProperty.Register(
        nameof(Valeur), typeof(decimal), typeof(CountUpText),
        new PropertyMetadata(0m, (d, _) => ((CountUpText)d).Animer()));

    public static readonly DependencyProperty SuffixeProperty = DependencyProperty.Register(
        nameof(Suffixe), typeof(string), typeof(CountUpText),
        new PropertyMetadata(string.Empty, (d, _) => ((CountUpText)d).MettreAJourTexte()));

    // Format numérique : "N0" (entier avec séparateur de milliers) par défaut, "N2" pour 2 décimales
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(CountUpText),
        new PropertyMetadata("N0", (d, _) => ((CountUpText)d).MettreAJourTexte()));

    private static readonly DependencyProperty ValeurAfficheeProperty = DependencyProperty.Register(
        nameof(ValeurAffichee), typeof(double), typeof(CountUpText),
        new PropertyMetadata(0.0, (d, _) => ((CountUpText)d).MettreAJourTexte()));

    public decimal Valeur
    {
        get => (decimal)GetValue(ValeurProperty);
        set => SetValue(ValeurProperty, value);
    }

    public string Suffixe
    {
        get => (string)GetValue(SuffixeProperty);
        set => SetValue(SuffixeProperty, value);
    }

    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    private double ValeurAffichee
    {
        get => (double)GetValue(ValeurAfficheeProperty);
        set => SetValue(ValeurAfficheeProperty, value);
    }

    public CountUpText()
    {
        MettreAJourTexte();

        // À l'affichage de l'écran, le compteur repart toujours de 0
        Loaded += (_, _) =>
        {
            BeginAnimation(ValeurAfficheeProperty, null);
            ValeurAffichee = 0;
            Animer();
        };
    }

    // Quand la valeur change plus tard (rafraîchissement), on part de ce qui est affiché
    private void Animer()
    {
        var animation = new DoubleAnimation(ValeurAffichee, (double)Valeur, TimeSpan.FromSeconds(1))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(ValeurAfficheeProperty, animation);
    }

    private void MettreAJourTexte()
        => Text = ValeurAffichee.ToString(Format, Francais) + Suffixe;
}