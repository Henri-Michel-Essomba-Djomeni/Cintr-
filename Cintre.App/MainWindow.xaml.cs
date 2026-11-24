using System.Windows;
using System.Windows.Controls;
using Cintre.App.Services;

namespace Cintre.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TxtPressing.Text = ParametresService.NomPressing;
        TxtUtilisateur.Text = Session.UtilisateurCourant?.Nom ?? "Non connecté";

        NavigationService.Initialiser(ZoneContenu);
        NavigationService.EcranChange += SynchroniserMenu;
        NavigationService.Naviguer("Dashboard");
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string ecran })
            NavigationService.Naviguer(ecran);
    }

    // Surligne l'entrée du menu qui correspond à l'écran affiché
    private void SynchroniserMenu(string ecran)
    {
        foreach (var bouton in PanneauMenu.Children.OfType<RadioButton>())
            bouton.IsChecked = (string?)bouton.Tag == ecran;
    }
}