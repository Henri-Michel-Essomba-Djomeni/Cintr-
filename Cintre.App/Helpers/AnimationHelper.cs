using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cintre.App.Helpers;

// Animations réutilisables par tous les écrans. Règle d'or : 200 à 600 ms, jamais bloquant.
public static class AnimationHelper
{
    private static readonly IEasingFunction Douceur = new CubicEase { EasingMode = EasingMode.EaseOut };

    // Apparition en fondu
    public static void FadeIn(FrameworkElement el, int ms = 300)
    {
        el.Opacity = 0;
        el.BeginAnimation(UIElement.OpacityProperty, CreerFondu(el, ms));
    }

    // Glissement de 20 px vers le haut avec fondu
    public static void SlideIn(FrameworkElement el, int ms = 350)
    {
        var translation = new TranslateTransform(0, 20);
        el.RenderTransform = translation;
        el.Opacity = 0;

        translation.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(ms)) { EasingFunction = Douceur });
        el.BeginAnimation(UIElement.OpacityProperty, CreerFondu(el, ms));
    }

    // Pulsation continue et discrète (alertes de retard)
    public static void Pulse(FrameworkElement el)
    {
        var animation = new DoubleAnimation(1, 0.55, TimeSpan.FromMilliseconds(800))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        el.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    // Arrête la pulsation et rend l'élément pleinement visible
    public static void ArreterPulse(FrameworkElement el)
    {
        el.BeginAnimation(UIElement.OpacityProperty, null);
        el.Opacity = 1;
    }

    private static DoubleAnimation CreerFondu(FrameworkElement el, int ms)
    {
        var fondu = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms)) { EasingFunction = Douceur };
        // À la fin, on libère la propriété pour que le code puisse encore changer l'opacité
        fondu.Completed += (_, _) =>
        {
            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 1;
        };
        return fondu;
    }
}