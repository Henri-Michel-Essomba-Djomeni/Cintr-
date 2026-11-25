using System.Windows;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        var vm = new LoginViewModel();
        vm.ConnexionReussie += () =>
        {
            new MainWindow().Show();   // d'abord ouvrir, ensuite fermer
            Close();
        };
        DataContext = vm;

        Loaded += (_, _) => TxtLogin.Focus();
    }
}