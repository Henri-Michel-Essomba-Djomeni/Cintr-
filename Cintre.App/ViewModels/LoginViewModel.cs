using System.Windows.Controls;
using Cintre.App.Helpers;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Propriétaire : Belvian
public class LoginViewModel : ViewModelBase
{
    private string _login = "";
    private string _messageErreur = "";

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string MessageErreur
    {
        get => _messageErreur;
        set => SetProperty(ref _messageErreur, value);
    }

    public RelayCommand ConnecterCommand { get; }

    // La fenêtre écoute cet événement pour ouvrir MainWindow
    public event Action? ConnexionReussie;

    public LoginViewModel()
    {
        ConnecterCommand = new RelayCommand(Connecter);
    }

    // Le mot de passe arrive en paramètre : un PasswordBox ne se lie pas directement
    private void Connecter(object? parametre)
    {
        MessageErreur = "";
        var motDePasse = (parametre as PasswordBox)?.Password ?? "";

        if (string.IsNullOrWhiteSpace(Login) || motDePasse.Length == 0)
        {
            MessageErreur = "Saisis ton identifiant et ton mot de passe.";
            return;
        }

        bool reussi;
        try
        {
            reussi = AuthService.Connecter(Login, motDePasse) != null;
        }
        catch (Exception)
        {
            MessageErreur = "Impossible de joindre la base de données. Vérifie Tailscale et l'adresse du serveur.";
            return;
        }

        if (!reussi)
        {
            // Un seul message, sans dire si c'est le login ou le mot de passe
            MessageErreur = "Identifiant ou mot de passe incorrect";
            return;
        }

        ConnexionReussie?.Invoke();
    }
}