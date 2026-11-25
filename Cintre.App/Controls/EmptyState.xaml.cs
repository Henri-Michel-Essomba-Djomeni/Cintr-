using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cintre.App.Helpers;

namespace Cintre.App.Controls;

// Écran vide illustré : titre, message, icône et un bouton d'action optionnel.
public partial class EmptyState : UserControl
{
    public static readonly DependencyProperty TitreProperty = DependencyProperty.Register(
        nameof(Titre), typeof(string), typeof(EmptyState), new PropertyMetadata("Rien à afficher pour le moment"));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconeProperty = DependencyProperty.Register(
        nameof(Icone), typeof(Geometry), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty ContenuActionProperty = DependencyProperty.Register(
        nameof(ContenuAction), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public string Titre
    {
        get => (string)GetValue(TitreProperty);
        set => SetValue(TitreProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public Geometry? Icone
    {
        get => (Geometry?)GetValue(IconeProperty);
        set => SetValue(IconeProperty, value);
    }

    // Un bouton (ou n'importe quel élément) affiché sous le message
    public object? ContenuAction
    {
        get => GetValue(ContenuActionProperty);
        set => SetValue(ContenuActionProperty, value);
    }

    public EmptyState()
    {
        InitializeComponent();
        Loaded += (_, _) => AnimationHelper.SlideIn(this, 400);
    }
}