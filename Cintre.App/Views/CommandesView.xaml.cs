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
        DataContext = new CommandesViewModel();
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