using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Propriétaire : Hugo
// Assistant de dépôt en 3 étapes : 1 Client, 2 Articles, 3 Récapitulatif.
//
// Les repositories de Raïssa (ClientRepository, TarifRepository) et FideliteService
// ne sont pas encore livrés : tant qu'ils lèvent NotImplementedException, ce ViewModel
// bascule tout seul sur des données de test. Après leur merge, le vrai code est utilisé
// sans rien changer ici.
public class DepotViewModel : ViewModelBase
{
    // ------------------------------------------------------------------
    // Données de test (utilisées seulement si les vrais repositories ne sont pas prêts)
    // ------------------------------------------------------------------

    private static readonly List<Client> _clientsTest = new()
    {
        new Client { Id = 1, Nom = "Aïcha Mbarga",  Telephone = "600000001", PointsFidelite = 50, CreeLe = DateTime.Now },
        new Client { Id = 2, Nom = "Jean Fotso",     Telephone = "600000002", PointsFidelite = 12, CreeLe = DateTime.Now },
        new Client { Id = 3, Nom = "Marie Nguema",   Telephone = "600000003", PointsFidelite = 0,  CreeLe = DateTime.Now },
    };

    private static readonly List<TypeVetement> _typesTest = new()
    {
        new TypeVetement { Id = 1, Nom = "Chemise" },
        new TypeVetement { Id = 2, Nom = "Pantalon" },
        new TypeVetement { Id = 3, Nom = "Robe" },
        new TypeVetement { Id = 4, Nom = "Costume" },
    };

    private static readonly List<Service> _servicesTest = new()
    {
        new Service { Id = 1, Nom = "Lavage" },
        new Service { Id = 2, Nom = "Repassage" },
        new Service { Id = 3, Nom = "Nettoyage à sec" },
    };

    // (serviceId, typeVetementId) -> prix. Les couples absents testent le message « pas proposé ».
    private static readonly Dictionary<(int, int), decimal> _prixTest = new()
    {
        { (1, 1), 500 },  { (1, 2), 600 },  { (1, 3), 800 },
        { (2, 1), 300 },  { (2, 2), 300 },  { (2, 3), 500 },  { (2, 4), 700 },
        { (3, 2), 1500 }, { (3, 3), 2500 }, { (3, 4), 3500 },
    };

    // ------------------------------------------------------------------
    // Accès aux données : vrai code d'abord, test sinon
    // ------------------------------------------------------------------

    private static List<Client> ChercherClients(string texte)
    {
        try { return ClientRepository.Rechercher(texte); }
        catch (NotImplementedException)
        {
            string t = (texte ?? "").Trim().ToLower();
            return _clientsTest
                .Where(c => t == "" || c.Nom.ToLower().Contains(t) || c.Telephone.Contains(t))
                .ToList();
        }
    }

    private static int AjouterClient(Client client)
    {
        try { return ClientRepository.Ajouter(client); }
        catch (NotImplementedException)
        {
            client.Id = _clientsTest.Max(c => c.Id) + 1;
            _clientsTest.Add(client);
            return client.Id;
        }
    }

    private static List<TypeVetement> ChargerTypes()
    {
        try { return TarifRepository.ObtenirTypesVetement(); }
        catch (NotImplementedException) { return _typesTest; }
    }

    private static List<Service> ChargerServices()
    {
        try { return TarifRepository.ObtenirServices().Where(s => s.Actif).ToList(); }
        catch (NotImplementedException) { return _servicesTest; }
    }

    private static decimal? ChercherPrix(int serviceId, int typeId)
    {
        try { return TarifRepository.ObtenirPrix(serviceId, typeId); }
        catch (NotImplementedException)
        {
            return _prixTest.TryGetValue((serviceId, typeId), out decimal p) ? p : null;
        }
    }

    private static bool PeutUtiliserRemise(Client c)
    {
        try { return FideliteService.PeutUtiliserRemise(c); }
        catch (NotImplementedException) { return c.PointsFidelite >= ParametresService.SeuilPointsRemise; }
    }

    private static decimal ObtenirRemise(Client c)
    {
        try { return FideliteService.ObtenirRemisePourcent(c); }
        catch (NotImplementedException) { return ParametresService.RemiseFideliteMax; }
    }

    private static void ConsommerPoints(int clientId)
    {
        try { FideliteService.UtiliserPoints(clientId); }
        catch (NotImplementedException) { /* pas encore livré : rien à faire pour l'instant */ }
    }

    // ------------------------------------------------------------------
    // État de l'assistant
    // ------------------------------------------------------------------

    private int _etape = 1;
    public int Etape
    {
        get => _etape;
        set
        {
            if (!SetProperty(ref _etape, value)) return;
            OnPropertyChanged(nameof(EstEtape1));
            OnPropertyChanged(nameof(EstEtape2));
            OnPropertyChanged(nameof(EstEtape3));
            OnPropertyChanged(nameof(EstEtapeNonFinale));
            OnPropertyChanged(nameof(TitreEtape));
            RaiserRecap();
        }
    }

    public bool EstEtape1 => Etape == 1;
    public bool EstEtape2 => Etape == 2;
    public bool EstEtape3 => Etape == 3;
    public bool EstEtapeNonFinale => Etape < 3;

    public string TitreEtape => Etape switch
    {
        1 => "Étape 1 sur 3 : Client",
        2 => "Étape 2 sur 3 : Articles",
        _ => "Étape 3 sur 3 : Récapitulatif"
    };

    private string _erreur = "";
    public string Erreur { get => _erreur; set => SetProperty(ref _erreur, value); }

    private string _info = "";
    public string Info { get => _info; set => SetProperty(ref _info, value); }

    // ------------------------------------------------------------------
    // Étape 1 : client
    // ------------------------------------------------------------------

    public ObservableCollection<Client> Resultats { get; } = new();

    private string _recherche = "";
    public string Recherche
    {
        get => _recherche;
        set
        {
            if (SetProperty(ref _recherche, value))
                RechargerClients();   // résultats immédiats à chaque lettre
        }
    }

    private Client? _clientChoisi;
    public Client? ClientChoisi
    {
        get => _clientChoisi;
        set
        {
            if (!SetProperty(ref _clientChoisi, value)) return;
            AppliquerRemise = true;
            OnPropertyChanged(nameof(ResumeClient));
            RaiserRecap();
        }
    }

    public string ResumeClient => ClientChoisi == null
        ? ""
        : $"Client : {ClientChoisi.Nom} ({ClientChoisi.Telephone}) · {ClientChoisi.PointsFidelite} points";

    // Mini formulaire « Nouveau client »
    private bool _afficherNouveauClient;
    public bool AfficherNouveauClient { get => _afficherNouveauClient; set => SetProperty(ref _afficherNouveauClient, value); }

    private string _nomNouveau = "";
    public string NomNouveau { get => _nomNouveau; set => SetProperty(ref _nomNouveau, value); }

    private string _telephoneNouveau = "";
    public string TelephoneNouveau { get => _telephoneNouveau; set => SetProperty(ref _telephoneNouveau, value); }

    // ------------------------------------------------------------------
    // Étape 2 : articles
    // ------------------------------------------------------------------

    public List<TypeVetement> TypesVetement { get; }
    public List<Service> Services { get; }
    public ObservableCollection<LigneCommande> Lignes { get; } = new();

    private TypeVetement? _typeChoisi;
    public TypeVetement? TypeChoisi
    {
        get => _typeChoisi;
        set { if (SetProperty(ref _typeChoisi, value)) MettreAJourPrix(); }
    }

    private Service? _serviceChoisi;
    public Service? ServiceChoisi
    {
        get => _serviceChoisi;
        set { if (SetProperty(ref _serviceChoisi, value)) MettreAJourPrix(); }
    }

    private decimal? _prixCourant;
    public decimal? PrixCourant
    {
        get => _prixCourant;
        private set
        {
            if (SetProperty(ref _prixCourant, value))
                OnPropertyChanged(nameof(PrixTexte));
        }
    }

    public string PrixTexte => PrixCourant.HasValue ? $"{PrixCourant.Value:N0} FCFA" : "—";

    private int _quantite = 1;
    public int Quantite { get => _quantite; set => SetProperty(ref _quantite, value); }

    private string _couleur = "";
    public string Couleur { get => _couleur; set => SetProperty(ref _couleur, value); }

    private string _remarqueLigne = "";
    public string RemarqueLigne { get => _remarqueLigne; set => SetProperty(ref _remarqueLigne, value); }

    // Photo de la ligne en cours de saisie (déjà réduite à 800 px, prête à être enregistrée)
    private byte[]? _photoCourante;
    public byte[]? PhotoCourante
    {
        get => _photoCourante;
        private set
        {
            if (!SetProperty(ref _photoCourante, value)) return;
            OnPropertyChanged(nameof(APhoto));
            OnPropertyChanged(nameof(PhotoTexte));
        }
    }

    public bool APhoto => PhotoCourante != null;
    public string PhotoTexte => APhoto ? "Photo ajoutée" : "Aucune photo";

    // ------------------------------------------------------------------
    // Étape 3 : récapitulatif
    // ------------------------------------------------------------------

    private bool _urgent;
    public bool Urgent
    {
        get => _urgent;
        set { if (SetProperty(ref _urgent, value)) RaiserRecap(); }
    }

    private bool _appliquerRemise = true;
    public bool AppliquerRemise
    {
        get => _appliquerRemise;
        set { if (SetProperty(ref _appliquerRemise, value)) RaiserRecap(); }
    }

    private string _remarqueGenerale = "";
    public string RemarqueGenerale { get => _remarqueGenerale; set => SetProperty(ref _remarqueGenerale, value); }

    // Tout est calculé par CommandeService : jamais de calcul dans la vue
    public bool RemiseDisponible => ClientChoisi != null && PeutUtiliserRemise(ClientChoisi);
    public decimal RemisePourcent => (RemiseDisponible && AppliquerRemise) ? ObtenirRemise(ClientChoisi!) : 0;
    public decimal Total => CommandeService.CalculerTotal(Lignes.ToList(), Urgent, RemisePourcent);
    public string TotalTexte => $"{Total:N0} FCFA";
    public string SousTotalTexte => $"{Lignes.Sum(l => l.SousTotal):N0} FCFA";
    public string DatePrevueTexte => CommandeService.CalculerDatePrevue(DateTime.Now, Urgent).ToString("dddd d MMMM yyyy");
    public string RemiseTexte => RemisePourcent > 0 ? $"Remise fidélité : -{RemisePourcent:0.##} %" : "";
    public string ResumeArticles => $"{Lignes.Sum(l => l.Quantite)} article(s) sur {Lignes.Count} ligne(s)";

    private void RaiserRecap()
    {
        OnPropertyChanged(nameof(RemiseDisponible));
        OnPropertyChanged(nameof(RemisePourcent));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalTexte));
        OnPropertyChanged(nameof(SousTotalTexte));
        OnPropertyChanged(nameof(DatePrevueTexte));
        OnPropertyChanged(nameof(RemiseTexte));
        OnPropertyChanged(nameof(ResumeArticles));
    }

    // ------------------------------------------------------------------
    // Commandes
    // ------------------------------------------------------------------

    public ICommand SuivantCommand { get; }
    public ICommand PrecedentCommand { get; }
    public ICommand BasculerNouveauClientCommand { get; }
    public ICommand CreerClientCommand { get; }
    public ICommand AjouterLigneCommand { get; }
    public ICommand SupprimerLigneCommand { get; }
    public ICommand ChoisirPhotoCommand { get; }
    public ICommand RetirerPhotoCommand { get; }
    public ICommand ValiderCommand { get; }

    public DepotViewModel()
    {
        TypesVetement = ChargerTypes();
        Services = ChargerServices();

        // le total et le récapitulatif suivent chaque ajout ou retrait de ligne
        Lignes.CollectionChanged += (_, _) => RaiserRecap();

        SuivantCommand = new RelayCommand(_ => Suivant(), _ => PeutAvancer());
        PrecedentCommand = new RelayCommand(_ => Etape--, _ => Etape > 1);
        BasculerNouveauClientCommand = new RelayCommand(_ => AfficherNouveauClient = !AfficherNouveauClient);
        CreerClientCommand = new RelayCommand(_ => CreerClient());
        AjouterLigneCommand = new RelayCommand(_ => AjouterLigne(), _ => PrixCourant != null && Quantite > 0);
        SupprimerLigneCommand = new RelayCommand(p => { if (p is LigneCommande l) Lignes.Remove(l); });
        ChoisirPhotoCommand = new RelayCommand(_ => ChoisirPhoto());
        RetirerPhotoCommand = new RelayCommand(_ => PhotoCourante = null, _ => APhoto);
        ValiderCommand = new RelayCommand(_ => Valider());

        RechargerClients();
    }

    private bool PeutAvancer() => Etape switch
    {
        1 => ClientChoisi != null,
        2 => Lignes.Count > 0,
        _ => false
    };

    private void Suivant()
    {
        Erreur = "";
        Info = "";
        if (Etape < 3) Etape++;
    }

    private void RechargerClients()
    {
        Resultats.Clear();
        foreach (var c in ChercherClients(Recherche))
            Resultats.Add(c);
    }

    private void CreerClient()
    {
        Erreur = "";
        if (string.IsNullOrWhiteSpace(NomNouveau) || string.IsNullOrWhiteSpace(TelephoneNouveau))
        {
            Erreur = "Le nom et le téléphone du client sont obligatoires.";
            return;
        }

        var client = new Client
        {
            Nom = NomNouveau.Trim(),
            Telephone = TelephoneNouveau.Trim(),
            CreeLe = DateTime.Now
        };

        try
        {
            client.Id = AjouterClient(client);
        }
        catch (Exception ex)
        {
            Erreur = "Impossible d'enregistrer le client : " + ex.Message;
            return;
        }

        Resultats.Add(client);
        ClientChoisi = client;
        NomNouveau = "";
        TelephoneNouveau = "";
        AfficherNouveauClient = false;
    }

    // Affiche le prix du couple (service, vêtement), ou le message prévu si le couple n'existe pas
    private void MettreAJourPrix()
    {
        Erreur = "";
        PrixCourant = null;
        if (TypeChoisi == null || ServiceChoisi == null) return;

        PrixCourant = ChercherPrix(ServiceChoisi.Id, TypeChoisi.Id);
        if (PrixCourant == null)
            Erreur = "Ce service n'est pas proposé pour ce vêtement";
    }

    // Ouvre le sélecteur de fichier, réduit l'image à 800 px de large maximum
    private void ChoisirPhoto()
    {
        var dialogue = new OpenFileDialog
        {
            Title = "Choisir une photo du vêtement",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialogue.ShowDialog() != true) return;

        try
        {
            PhotoCourante = ImageHelper.Redimensionner(dialogue.FileName, 800);
            Erreur = "";
        }
        catch (Exception)
        {
            Erreur = "Impossible de lire cette image. Choisis un fichier JPG, PNG ou BMP.";
        }
    }

    private void AjouterLigne()
    {
        if (TypeChoisi == null || ServiceChoisi == null || PrixCourant == null) return;

        Lignes.Add(new LigneCommande
        {
            TypeVetementId = TypeChoisi.Id,
            ServiceId = ServiceChoisi.Id,
            Quantite = Quantite,
            PrixUnitaire = PrixCourant.Value,   // copié maintenant : un futur changement de tarif ne touche pas cette commande
            Couleur = string.IsNullOrWhiteSpace(Couleur) ? null : Couleur.Trim(),
            Remarque = string.IsNullOrWhiteSpace(RemarqueLigne) ? null : RemarqueLigne.Trim(),
            Photo = PhotoCourante,
            TypeVetementNom = TypeChoisi.Nom,
            ServiceNom = ServiceChoisi.Nom
        });

        Quantite = 1;
        Couleur = "";
        RemarqueLigne = "";
        PhotoCourante = null;
        Erreur = "";
    }

    private void Valider()
    {
        Erreur = "";
        Info = "";

        if (ClientChoisi == null || Lignes.Count == 0)
        {
            Erreur = "Choisis un client et ajoute au moins un article.";
            return;
        }

        var utilisateur = Session.UtilisateurCourant;
        if (utilisateur == null)
        {
            Erreur = "Aucun utilisateur connecté.";
            return;
        }

        DateTime maintenant = DateTime.Now;
        var commande = new Commande
        {
            // NumeroTicket vide : CommandeRepository.Creer le génère
            ClientId = ClientChoisi.Id,
            UtilisateurId = utilisateur.Id,
            DateDepot = maintenant,
            DatePrevue = CommandeService.CalculerDatePrevue(maintenant, Urgent),
            Statut = StatutCommande.Recu,
            Urgent = Urgent,
            RemisePourcent = RemisePourcent,
            MontantTotal = Total,
            MontantPaye = 0,
            Remarque = string.IsNullOrWhiteSpace(RemarqueGenerale) ? null : RemarqueGenerale.Trim(),
            Lignes = Lignes.ToList()
        };

        try
        {
            CommandeRepository.Creer(commande);
        }
        catch (Exception ex)
        {
            Erreur = "Impossible d'enregistrer le dépôt : " + ex.Message;
            return;
        }

        // la remise a été utilisée : les points sont consommés
        if (commande.RemisePourcent > 0)
            ConsommerPoints(commande.ClientId);

        Reinitialiser();
        Info = $"Dépôt enregistré : {commande.NumeroTicket} · total {commande.MontantTotal:N0} FCFA";
    }

    // Retour à l'étape 1, prêt pour le client suivant
    private void Reinitialiser()
    {
        Lignes.Clear();
        ClientChoisi = null;
        Urgent = false;
        AppliquerRemise = true;
        RemarqueGenerale = "";
        Quantite = 1;
        Couleur = "";
        RemarqueLigne = "";
        PhotoCourante = null;
        TypeChoisi = null;
        ServiceChoisi = null;
        Recherche = "";
        RechargerClients();
        Etape = 1;
    }
}
