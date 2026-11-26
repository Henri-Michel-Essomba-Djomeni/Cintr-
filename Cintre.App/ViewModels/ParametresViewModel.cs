using System.Globalization;
using System.Windows;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Services;
using Cintre.App.Views;

namespace Cintre.App.ViewModels;

// Propriétaire : Belvian
public class ParametresViewModel : ViewModelBase
{
    private string _nomPressing = "", _adresse = "", _telephone = "", _devise = "";
    private string _delai = "", _supplement = "", _seuil = "", _remise = "", _jours = "";
    private string _messageErreur = "";

    public string NomPressing { get => _nomPressing; set => SetProperty(ref _nomPressing, value); }
    public string Adresse { get => _adresse; set => SetProperty(ref _adresse, value); }
    public string Telephone { get => _telephone; set => SetProperty(ref _telephone, value); }
    public string Devise { get => _devise; set => SetProperty(ref _devise, value); }

    public string DelaiStandardJours { get => _delai; set => SetProperty(ref _delai, value); }
    public string SupplementUrgencePourcent { get => _supplement; set => SetProperty(ref _supplement, value); }
    public string SeuilPointsRemise { get => _seuil; set => SetProperty(ref _seuil, value); }
    public string RemiseFidelitePourcent { get => _remise; set => SetProperty(ref _remise, value); }
    public string JoursAbandon { get => _jours; set => SetProperty(ref _jours, value); }

    public string MessageErreur { get => _messageErreur; set => SetProperty(ref _messageErreur, value); }

    public RelayCommand EnregistrerCommand { get; }
    public RelayCommand OuvrirServeurCommand { get; }

    public ParametresViewModel()
    {
        EnregistrerCommand = new RelayCommand(_ => Enregistrer());
        OuvrirServeurCommand = new RelayCommand(_ => OuvrirServeur());
        Charger();
    }

    private void Charger()
    {
        try
        {
            ParametresService.Recharger();
        }
        catch (Exception)
        {
            MessageErreur = "Impossible de lire les paramètres dans la base : valeurs par défaut affichées.";
        }

        NomPressing = ParametresService.NomPressing;
        Adresse = ParametresService.Adresse;
        Telephone = ParametresService.Telephone;
        Devise = ParametresService.Devise;

        DelaiStandardJours = ParametresService.DelaiStandardJours.ToString(CultureInfo.InvariantCulture);
        SupplementUrgencePourcent = ParametresService.SupplementUrgencePourcent.ToString(CultureInfo.InvariantCulture);
        SeuilPointsRemise = ParametresService.SeuilPointsRemise.ToString(CultureInfo.InvariantCulture);
        RemiseFidelitePourcent = ParametresService.RemiseFideliteMax.ToString(CultureInfo.InvariantCulture);
        JoursAbandon = ParametresService.JoursAbandon.ToString(CultureInfo.InvariantCulture);
    }

    private void OuvrirServeur()
    {
        var fenetre = new ConnexionBaseWindow();

        // On cherche la vraie fenêtre principale : Application.Current.MainWindow
        // peut être vide ou pointer sur la fenêtre qu'on vient de créer
        var principale = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (principale != null)
            fenetre.Owner = principale;

        if (fenetre.ShowDialog() == true)
        {
            Charger();   // relit les paramètres depuis le serveur choisi
            MessageErreur = "";
            ToastService.Succes("Connexion à la base enregistrée.");
        }
    }

    private void Enregistrer()
    {
        MessageErreur = "";

        if (string.IsNullOrWhiteSpace(NomPressing))
        {
            MessageErreur = "Le nom du pressing est obligatoire.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Devise))
        {
            MessageErreur = "La devise est obligatoire.";
            return;
        }
        if (!LireEntier(DelaiStandardJours, 1, 365, out var delai))
        {
            MessageErreur = "Le délai standard doit être un nombre entier de jours (1 à 365).";
            return;
        }
        if (!LireNombre(SupplementUrgencePourcent, 0, 100, out var supplement))
        {
            MessageErreur = "Le supplément d'urgence doit être un pourcentage entre 0 et 100.";
            return;
        }
        if (!LireEntier(SeuilPointsRemise, 1, 100000, out var seuil))
        {
            MessageErreur = "Le seuil de points doit être un nombre entier positif.";
            return;
        }
        if (!LireNombre(RemiseFidelitePourcent, 0, 100, out var remise))
        {
            MessageErreur = "La remise de fidélité doit être un pourcentage entre 0 et 100.";
            return;
        }
        if (!LireEntier(JoursAbandon, 1, 3650, out var jours))
        {
            MessageErreur = "Le nombre de jours d'abandon doit être un entier positif.";
            return;
        }

        try
        {
            ParametreRepository.Definir("nom_pressing", NomPressing.Trim());
            ParametreRepository.Definir("adresse", Adresse?.Trim() ?? "");
            ParametreRepository.Definir("telephone", Telephone?.Trim() ?? "");
            ParametreRepository.Definir("devise", Devise.Trim());
            ParametreRepository.Definir("delai_standard_jours", delai.ToString(CultureInfo.InvariantCulture));
            ParametreRepository.Definir("supplement_urgence_pourcent", supplement.ToString(CultureInfo.InvariantCulture));
            ParametreRepository.Definir("seuil_points_remise", seuil.ToString(CultureInfo.InvariantCulture));
            ParametreRepository.Definir("remise_fidelite_pourcent", remise.ToString(CultureInfo.InvariantCulture));
            ParametreRepository.Definir("jours_abandon", jours.ToString(CultureInfo.InvariantCulture));

            ParametresService.Recharger();   // les autres modules voient les nouvelles valeurs
            ToastService.Succes("Paramètres enregistrés.");
        }
        catch (Exception)
        {
            MessageErreur = "Enregistrement impossible : la base ne répond pas.";
        }
    }

    private static bool LireEntier(string texte, int min, int max, out int valeur)
        => int.TryParse(texte?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out valeur)
           && valeur >= min && valeur <= max;

    // Accepte "12.5" et "12,5"
    private static bool LireNombre(string texte, decimal min, decimal max, out decimal valeur)
        => decimal.TryParse(texte?.Trim().Replace(',', '.'), NumberStyles.Number,
                            CultureInfo.InvariantCulture, out valeur)
           && valeur >= min && valeur <= max;
}