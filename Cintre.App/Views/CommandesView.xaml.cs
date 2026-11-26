using Cintre.App.Helpers;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class CommandesView : UserControl
{
    public CommandesView()
    {
        InitializeComponent();
        AnimationHelper.FadeIn(this);
        DataContext = new CommandesViewModel();
    }
    // le badge rouge "En retard" pulse doucement pour attirer l'oeil
    private void BadgeRetard_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        AnimationHelper.Pulse((System.Windows.FrameworkElement)sender);
    }
}

// Affiche les statuts en français lisible
public class StatutTexteConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => (value?.ToString()) switch
    {
        "Recu" => "Reçu", "EnTraitement" => "En traitement", "Pret" => "Prêt",
        "Retire" => "Retiré", "Annule" => "Annulé", _ => value?.ToString() ?? ""
    };
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}