using System.Windows;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

// S'affiche avec ShowDialog() : retourne true quand une connexion valide a été enregistrée.
public partial class ConnexionBaseWindow : Window
{
    private readonly ConnexionBaseViewModel _vm = new();

    public ConnexionBaseWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        // Un PasswordBox ne se lie pas directement : on le relie à la main
        ChampMdp.Password = _vm.MotDePasse;
        ChampMdp.PasswordChanged += (_, _) => _vm.MotDePasse = ChampMdp.Password;

        _vm.DemandeFermeture += ok => DialogResult = ok;
    }
}