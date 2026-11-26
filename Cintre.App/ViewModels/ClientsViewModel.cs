using System.Collections.ObjectModel;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une carte de la liste (le client + son nombre de commandes)
public class ClientCarte
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Telephone { get; set; } = "";
    public int PointsFidelite { get; set; }
    public int NbCommandes { get; set; }
}

// Propriétaire : Raïssa
public class ClientsViewModel : ViewModelBase
{
    private int _id;                      // 0 = nouveau client
    private string _recherche = "";
    private ClientCarte? _selection;
    private string _nom = "", _telephone = "", _email = "", _adresse = "";
    private string _erreurNom = "", _erreurTelephone = "";
    private string _titreFiche = "Nouveau client";
    private int _points;
    private string _messageHistorique = "";

    public ObservableCollection<ClientCarte> Clients { get; } = new();
    public ObservableCollection<Commande> Historique { get; } = new();

    public RelayCommand NouveauCommand { get; }
    public RelayCommand EnregistrerCommand { get; }
    public RelayCommand SupprimerCommand { get; }

    public ClientsViewModel()
    {
        NouveauCommand = new RelayCommand(_ => NouveauClient());
        EnregistrerCommand = new RelayCommand(_ => Enregistrer());
        SupprimerCommand = new RelayCommand(_ => Supprimer(), _ => !EstNouveau);

        Charger();
        NouveauClient();
    }

    // ---------- Recherche et liste ----------
    public string Recherche
    {
        get => _recherche;
        set
        {
            if (SetProperty(ref _recherche, value))
            {
                OnPropertyChanged(nameof(IndiceVisible));
                Charger();
            }
        }
    }

    public bool IndiceVisible => string.IsNullOrEmpty(_recherche);
    public bool AucunResultat => Clients.Count == 0;

    public ClientCarte? Selection
    {
        get => _selection;
        set
        {
            if (SetProperty(ref _selection, value) && value != null)
                ChargerFiche(value.Id);
        }
    }

    // ---------- Fiche ----------
    public string Nom
    {
        get => _nom;
        set { if (SetProperty(ref _nom, value)) ErreurNom = ""; }
    }

    public string Telephone
    {
        get => _telephone;
        set { if (SetProperty(ref _telephone, value)) ErreurTelephone = ""; }
    }

    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Adresse { get => _adresse; set => SetProperty(ref _adresse, value); }

    public string ErreurNom { get => _erreurNom; set => SetProperty(ref _erreurNom, value); }
    public string ErreurTelephone { get => _erreurTelephone; set => SetProperty(ref _erreurTelephone, value); }

    public string TitreFiche { get => _titreFiche; private set => SetProperty(ref _titreFiche, value); }
    public bool EstNouveau => _id == 0;
    public bool PeutSupprimer => _id != 0;

    // ---------- Fidélité ----------
    public int Points => _points;

    public bool EstEligible =>
        !EstNouveau && FideliteService.PeutUtiliserRemise(new Client { PointsFidelite = _points });

    public string TexteFidelite
    {
        get
        {
            if (EstNouveau)
                return "Le client gagne 1 point par tranche de 1 000 FCFA dépensée.";
            if (EstEligible)
                return $"Remise de {ParametresService.RemiseFideliteMax} % disponible sur la prochaine commande.";
            var reste = ParametresService.SeuilPointsRemise - _points;
            return $"Encore {reste} point(s) avant la remise de {ParametresService.RemiseFideliteMax} %.";
        }
    }

    // ---------- Historique ----------
    public bool HistoriqueDisponible => Historique.Count > 0;
    public string MessageHistorique { get => _messageHistorique; private set => SetProperty(ref _messageHistorique, value); }

    // ---------- Actions ----------
    private void Charger()
    {
        try
        {
            var clients = ClientRepository.Rechercher(Recherche);
            var nb = ClientRepository.CompterCommandesParClient();
            Clients.Clear();
            foreach (var c in clients)
            {
                Clients.Add(new ClientCarte
                {
                    Id = c.Id,
                    Nom = c.Nom,
                    Telephone = c.Telephone,
                    PointsFidelite = c.PointsFidelite,
                    NbCommandes = nb.TryGetValue(c.Id, out var n) ? n : 0
                });
            }
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les clients : " + ex.Message);
        }

        // On garde la carte du client en cours de modification, si elle est encore dans la liste
        _selection = _id == 0 ? null : Clients.FirstOrDefault(c => c.Id == _id);
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(AucunResultat));
    }

    private void NouveauClient()
    {
        _id = 0;
        _selection = null;
        OnPropertyChanged(nameof(Selection));

        Nom = "";
        Telephone = "";
        Email = "";
        Adresse = "";
        ErreurNom = "";
        ErreurTelephone = "";
        _points = 0;
        TitreFiche = "Nouveau client";

        Historique.Clear();
        MessageHistorique = "Enregistrez le client pour voir son historique.";
        MajEtat();
    }

    private void ChargerFiche(int id)
    {
        Client? c;
        try
        {
            c = ClientRepository.ObtenirParId(id);
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger le client : " + ex.Message);
            return;
        }

        if (c == null)
        {
            // Supprimé entre-temps par un autre poste
            MessageService.Info("Ce client n'existe plus.");
            NouveauClient();
            Charger();
            return;
        }

        _id = c.Id;
        Nom = c.Nom;
        Telephone = c.Telephone;
        Email = c.Email ?? "";
        Adresse = c.Adresse ?? "";
        ErreurNom = "";
        ErreurTelephone = "";
        _points = c.PointsFidelite;
        TitreFiche = c.Nom;

        ChargerHistorique(c.Id);
        MajEtat();
    }

    private void ChargerHistorique(int clientId)
    {
        Historique.Clear();
        try
        {
            foreach (var commande in CommandeRepository.ObtenirParClient(clientId))
                Historique.Add(commande);
            MessageHistorique = Historique.Count == 0 ? "Ce client n'a pas encore de commande." : "";
        }
        catch (NotImplementedException)
        {
            // Hugo n'a pas encore livré CommandeRepository.ObtenirParClient
            MessageHistorique = "L'historique sera disponible dès que le module Commandes sera branché.";
        }
        catch (Exception ex)
        {
            MessageHistorique = "Impossible de charger l'historique : " + ex.Message;
        }
        OnPropertyChanged(nameof(HistoriqueDisponible));
    }

    private void Enregistrer()
    {
        ErreurNom = string.IsNullOrWhiteSpace(Nom) ? "Le nom est obligatoire." : "";
        ErreurTelephone = string.IsNullOrWhiteSpace(Telephone) ? "Le téléphone est obligatoire." : "";
        if (ErreurNom != "" || ErreurTelephone != "")
            return;

        var client = new Client
        {
            Id = _id,
            Nom = Nom.Trim(),
            Telephone = Telephone.Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Adresse = string.IsNullOrWhiteSpace(Adresse) ? null : Adresse.Trim()
        };

        try
        {
            if (EstNouveau)
            {
                _id = ClientRepository.Ajouter(client);
                MessageService.Succes("Client ajouté.");
            }
            else
            {
                ClientRepository.Modifier(client);
                MessageService.Succes("Client modifié.");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Téléphone déjà utilisé : on l'affiche sous le champ
            ErreurTelephone = ex.Message;
            return;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible d'enregistrer : " + ex.Message);
            return;
        }

        Charger();
        ChargerFiche(_id);
    }

    private void Supprimer()
    {
        if (EstNouveau)
            return;

        if (!MessageService.Confirmer($"Supprimer le client {Nom} ?", "Supprimer", danger: true))
            return;

        try
        {
            ClientRepository.Supprimer(_id);
        }
        catch (InvalidOperationException ex)
        {
            // Le client a des commandes
            MessageService.Erreur(ex.Message);
            return;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de supprimer : " + ex.Message);
            return;
        }

        MessageService.Succes("Client supprimé.");
        NouveauClient();
        Charger();
    }

    // Prévient l'écran que les valeurs calculées ont changé
    private void MajEtat()
    {
        OnPropertyChanged(nameof(EstNouveau));
        OnPropertyChanged(nameof(PeutSupprimer));
        OnPropertyChanged(nameof(Points));
        OnPropertyChanged(nameof(EstEligible));
        OnPropertyChanged(nameof(TexteFidelite));
        OnPropertyChanged(nameof(HistoriqueDisponible));
    }
}
