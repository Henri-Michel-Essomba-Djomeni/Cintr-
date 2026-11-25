using System.Collections.ObjectModel;
using System.Windows.Input;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

public class ConnexionBaseViewModel : ViewModelBase
{
    private string _hote = string.Empty;
    private string _portTexte = string.Empty;
    private string _nomBase = string.Empty;
    private string _utilisateur = string.Empty;
    private string _message = string.Empty;
    private bool _messageEstSucces;
    private bool _enCours;
    private bool _adressesVisibles;

    // true = connexion enregistrée, la fenêtre peut se fermer
    public event Action<bool>? DemandeFermeture;

    public ConnexionBaseViewModel()
    {
        var config = ConfigService.Obtenir();
        _hote = config.Host;
        _portTexte = config.Port.ToString();
        _nomBase = config.Base;
        _utilisateur = config.Utilisateur;
        MotDePasse = config.MotDePasse; // transmis par la fenêtre (un PasswordBox ne se lie pas)

        CommandeTester = new RelayCommand(async _ => await TesterAsync(), _ => !EnCours);
        CommandeEnregistrer = new RelayCommand(async _ => await EnregistrerAsync(), _ => !EnCours);
        CommandeJeSuisHote = new RelayCommand(_ => JeSuisHote(), _ => !EnCours);
        CommandeAfficherAdresse = new RelayCommand(_ => AfficherAdresse());
    }

    public string Hote { get => _hote; set => SetProperty(ref _hote, value); }
    public string PortTexte { get => _portTexte; set => SetProperty(ref _portTexte, value); }
    public string NomBase { get => _nomBase; set => SetProperty(ref _nomBase, value); }
    public string Utilisateur { get => _utilisateur; set => SetProperty(ref _utilisateur, value); }
    public string MotDePasse { get; set; }

    public string Message
    {
        get => _message;
        private set
        {
            if (SetProperty(ref _message, value))
                OnPropertyChanged(nameof(AMessage));
        }
    }

    public bool AMessage => !string.IsNullOrEmpty(Message);

    public bool MessageEstSucces { get => _messageEstSucces; private set => SetProperty(ref _messageEstSucces, value); }

    public bool EnCours
    {
        get => _enCours;
        private set
        {
            if (SetProperty(ref _enCours, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool AdressesVisibles { get => _adressesVisibles; private set => SetProperty(ref _adressesVisibles, value); }

    public ObservableCollection<string> Adresses { get; } = new();

    public ICommand CommandeTester { get; }
    public ICommand CommandeEnregistrer { get; }
    public ICommand CommandeJeSuisHote { get; }
    public ICommand CommandeAfficherAdresse { get; }

    private void Afficher(string message, bool succes)
    {
        MessageEstSucces = succes;
        Message = message;
    }

    // Vérifie les champs ; les erreurs s'affichent sous le formulaire, jamais dans une boîte de dialogue
    private ConfigBase? Construire()
    {
        if (string.IsNullOrWhiteSpace(Hote))
        {
            Afficher("Saisis l'adresse de l'hôte.", false);
            return null;
        }
        if (!int.TryParse(PortTexte, out var port) || port < 1 || port > 65535)
        {
            Afficher("Le port doit être un nombre entre 1 et 65535.", false);
            return null;
        }
        if (string.IsNullOrWhiteSpace(NomBase) || string.IsNullOrWhiteSpace(Utilisateur))
        {
            Afficher("Le nom de la base et l'utilisateur sont obligatoires.", false);
            return null;
        }

        return new ConfigBase
        {
            Host = Hote.Trim(),
            Port = port,
            Base = NomBase.Trim(),
            Utilisateur = Utilisateur.Trim(),
            MotDePasse = MotDePasse ?? string.Empty
        };
    }

    // Le test peut durer jusqu'à 5 s : on le lance en tâche de fond pour ne pas figer la fenêtre
    private async Task<(bool ok, string message, ConfigBase? config)> TesterConfigAsync()
    {
        var config = Construire();
        if (config == null) return (false, string.Empty, null);

        EnCours = true;
        Message = string.Empty;
        try
        {
            var (ok, message) = await Task.Run(() => DbConnectionFactory.Tester(config));
            return (ok, message, config);
        }
        finally
        {
            EnCours = false;
        }
    }

    private async Task TesterAsync()
    {
        var (ok, message, config) = await TesterConfigAsync();
        if (config == null) return; // champ invalide : le message est déjà affiché
        Afficher(message, ok);
    }

    private async Task EnregistrerAsync()
    {
        var (ok, message, config) = await TesterConfigAsync();
        if (config == null) return;

        if (!ok)
        {
            Afficher(message, false);
            return;
        }

        try
        {
            ConfigService.Enregistrer(config);
        }
        catch (Exception ex)
        {
            Afficher("Impossible d'enregistrer la configuration : " + ex.Message, false);
            return;
        }

        Afficher("Connexion réussie. Configuration enregistrée.", true);
        await Task.Delay(800); // laisse le temps de lire le message vert
        DemandeFermeture?.Invoke(true);
    }

    private void JeSuisHote()
    {
        Hote = "localhost";
        Message = string.Empty;
    }

    private void AfficherAdresse()
    {
        Adresses.Clear();
        var ips = ReseauHelper.ObtenirAdressesIp();

        if (ips.Count == 0)
            Adresses.Add("Aucune adresse réseau trouvée.");

        foreach (var ip in ips)
            Adresses.Add(ReseauHelper.EstAdresseTailscale(ip) ? $"{ip}   ·   Tailscale (à donner aux autres)" : ip);

        AdressesVisibles = true;
    }
}