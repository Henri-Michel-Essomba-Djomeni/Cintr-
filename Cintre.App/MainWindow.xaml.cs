using System.Windows;
using System.Windows.Controls;
using Cintre.App.Services;

namespace Cintre.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TxtPressing.Text = ParametresService.NomPressing == "Cintré"
            ? "Gestion de pressing"
            : ParametresService.NomPressing;

        var nom = Session.UtilisateurCourant?.Nom;
        TxtUtilisateur.Text = nom ?? "Non connecté";
        TxtInitiale.Text = string.IsNullOrWhiteSpace(nom) ? "?" : nom.Trim()[..1].ToUpper();

        NavigationService.Initialiser(ZoneContenu);
        ToastService.Initialiser(ZoneToasts);
        NavigationService.EcranChange += SynchroniserMenu;
        NavigationService.Naviguer("Dashboard");
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string ecran })
            NavigationService.Naviguer(ecran);
    }

    private void Deconnexion_Click(object sender, RoutedEventArgs e)
    {
        // Branché sur AuthService.Deconnecter par Belvian (T11/T12)
        MessageService.Info("La déconnexion sera active avec l'authentification.");
    }

    private void SynchroniserMenu(string ecran)
    {
        foreach (var bouton in PanneauMenu.Children.OfType<RadioButton>())
            bouton.IsChecked = (string?)bouton.Tag == ecran;
    }
}