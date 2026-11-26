using System.Collections.ObjectModel;
using System.Globalization;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une ligne de la liste des incidents
public class LigneIncident
{
    public int Id { get; set; }
    public string Ticket { get; set; } = "";
    public string Client { get; set; } = "";
    public string Type { get; set; } = "";
    public string Date { get; set; } = "";
    public string Statut { get; set; } = "";
    public string Description { get; set; } = "";
    public string Resolution { get; set; } = "";
    public bool Resolu { get; set; }
}

// Une ligne de la liste du linge abandonné
public class LigneAbandon
{
    public string Ticket { get; set; } = "";
    public string Client { get; set; } = "";
    public string PretDepuis { get; set; } = "";
    public string Reste { get; set; } = "";
}

// Propriétaire : Valdez
public class IncidentsViewModel : ViewModelBase
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private string _ticketSaisi = "";
    private string _descriptionSaisie = "";
    private string _resolutionSaisie = "";
    private TypeIncident _typeChoisi;
    private bool _seulementOuverts = true;
    private bool _ongletAbandons;
    private LigneIncident? _incidentSelectionne;
    private string _messageErreur = "";
    private string _messageSucces = "";

    public ObservableCollection<LigneIncident> Incidents { get; } = new();
    public ObservableCollection<LigneAbandon> Abandonnes { get; } = new();

    // Les types viennent directement de l'enum : aucun nom écrit en dur
    public IEnumerable<TypeIncident> Types { get; } = Enum.GetValues<TypeIncident>();

    public RelayCommand DeclarerCommand { get; }
    public RelayCommand ResoudreCommand { get; }

    public IncidentsViewModel()
    {
        _typeChoisi = Types.First();

        DeclarerCommand = new RelayCommand(_ => Declarer());
        ResoudreCommand = new RelayCommand(_ => Resoudre());

        ChargerListes();
    }

    // ===== Saisies =====

    public string TicketSaisi
    {
        get => _ticketSaisi;
        set => SetProperty(ref _ticketSaisi, value);
    }

    public TypeIncident TypeChoisi
    {
        get => _typeChoisi;
        set => SetProperty(ref _typeChoisi, value);
    }

    public string DescriptionSaisie
    {
        get => _descriptionSaisie;
        set => SetProperty(ref _descriptionSaisie, value);
    }

    public string ResolutionSaisie
    {
        get => _resolutionSaisie;
        set => SetProperty(ref _resolutionSaisie, value);
    }

    // ===== Filtre =====

    public bool SeulementOuverts
    {
        get => _seulementOuverts;
        set
        {
            if (SetProperty(ref _seulementOuverts, value))
                ChargerListes();
        }
    }

    // ===== Onglets =====

    public bool OngletIncidents
    {
        get => !_ongletAbandons;
        set { if (value) ChoisirOnglet(false); }
    }

    public bool OngletAbandons
    {
        get => _ongletAbandons;
        set { if (value) ChoisirOnglet(true); }
    }

    private void ChoisirOnglet(bool abandons)
    {
        _ongletAbandons = abandons;
        OnPropertyChanged(nameof(OngletIncidents));
        OnPropertyChanged(nameof(OngletAbandons));
    }

    // ===== Sélection =====

    public LigneIncident? IncidentSelectionne
    {
        get => _incidentSelectionne;
        set
        {
            if (SetProperty(ref _incidentSelectionne, value))
            {
                OnPropertyChanged(nameof(ASelection));
                OnPropertyChanged(nameof(SelectionTitre));
                OnPropertyChanged(nameof(SelectionDescription));
                OnPropertyChanged(nameof(SelectionResolution));
                OnPropertyChanged(nameof(PeutResoudre));
            }
        }
    }

    public bool ASelection => _incidentSelectionne != null;

    public bool PeutResoudre => _incidentSelectionne != null && !_incidentSelectionne.Resolu;

    public string SelectionTitre => _incidentSelectionne == null
        ? ""
        : $"{_incidentSelectionne.Ticket} · {_incidentSelectionne.Client} · {_incidentSelectionne.Type}";

    public string SelectionDescription => _incidentSelectionne?.Description ?? "";

    public string SelectionResolution => _incidentSelectionne == null
        ? ""
        : _incidentSelectionne.Resolu
            ? "Résolution : " + _incidentSelectionne.Resolution
            : "Incident encore ouvert.";

    // ===== Textes et états des listes =====

    public bool SansIncident => Incidents.Count == 0;
    public bool SansAbandon => Abandonnes.Count == 0;

    public string AbandonLegende =>
        $"Commandes prêtes depuis plus de {ParametresService.JoursAbandon} jours et jamais retirées.";

    // ===== Messages =====

    public string MessageErreur
    {
        get => _messageErreur;
        private set
        {
            if (SetProperty(ref _messageErreur, value))
                OnPropertyChanged(nameof(AErreur));
        }
    }

    public bool AErreur => !string.IsNullOrEmpty(_messageErreur);

    public string MessageSucces
    {
        get => _messageSucces;
        private set
        {
            if (SetProperty(ref _messageSucces, value))
                OnPropertyChanged(nameof(ASucces));
        }
    }

    public bool ASucces => !string.IsNullOrEmpty(_messageSucces);

    // ===== Actions =====

    private void Declarer()
    {
        MessageErreur = "";
        MessageSucces = "";

        var ticket = (TicketSaisi ?? "").Trim().ToUpperInvariant();
        if (ticket.Length == 0)
        {
            MessageErreur = "Saisis le numéro de ticket de la commande concernée.";
            return;
        }

        var description = (DescriptionSaisie ?? "").Trim();
        if (description.Length == 0)
        {
            MessageErreur = "Décris brièvement l'incident.";
            return;
        }

        try
        {
            var commande = IncidentRepository.ObtenirCommandeParTicket(ticket);
            if (commande == null)
            {
                MessageErreur = $"Aucune commande trouvée pour le ticket {ticket}.";
                return;
            }

            IncidentRepository.Declarer(new Incident
            {
                CommandeId = commande.Id,
                Type = TypeChoisi,
                Description = description
            });

            TicketSaisi = "";
            DescriptionSaisie = "";
            MessageService.Succes($"Incident enregistré pour le ticket {ticket}.");
            ChargerListes();
        }
        catch (Exception ex)
        {
            MessageErreur = "Déclaration impossible : " + ex.Message;
        }
    }

    private void Resoudre()
    {
        MessageErreur = "";
        MessageSucces = "";

        var selection = IncidentSelectionne;
        if (selection == null)
        {
            MessageErreur = "Sélectionne d'abord un incident dans la liste.";
            return;
        }

        if (selection.Resolu)
        {
            MessageErreur = "Cet incident est déjà résolu.";
            return;
        }

        var resolution = (ResolutionSaisie ?? "").Trim();
        if (resolution.Length == 0)
        {
            MessageErreur = "Explique comment l'incident a été résolu.";
            return;
        }

        try
        {
            IncidentRepository.Resoudre(selection.Id, resolution);
            ResolutionSaisie = "";
            MessageService.Succes($"Incident du ticket {selection.Ticket} marqué comme résolu.");
            ChargerListes();
        }
        catch (Exception ex)
        {
            MessageErreur = "Résolution impossible : " + ex.Message;
        }
    }

    // ===== Chargement =====

    private void ChargerListes()
    {
        IncidentSelectionne = null;

        try
        {
            Incidents.Clear();
            foreach (var i in IncidentRepository.ObtenirTous(_seulementOuverts))
            {
                Incidents.Add(new LigneIncident
                {
                    Id = i.Id,
                    Ticket = i.NumeroTicket,
                    Client = i.ClientNom,
                    Type = i.Type.ToString(),
                    Date = i.DateIncident.ToString("dd/MM/yyyy HH:mm", Fr),
                    Statut = i.Resolu ? "Résolu" : "Ouvert",
                    Description = i.Description,
                    Resolution = i.Resolution ?? "",
                    Resolu = i.Resolu
                });
            }

            Abandonnes.Clear();
            foreach (var c in IncidentRepository.ObtenirCommandesAbandonnees(ParametresService.JoursAbandon))
            {
                Abandonnes.Add(new LigneAbandon
                {
                    Ticket = c.NumeroTicket,
                    Client = c.ClientNom,
                    PretDepuis = $"{c.DatePrevue:dd/MM/yyyy}",
                    Reste = c.Reste.ToString("N0", Fr) + " FCFA"
                });
            }
        }
        catch (Exception ex)
        {
            // Une base injoignable ne doit jamais faire planter l'écran
            Incidents.Clear();
            Abandonnes.Clear();
            MessageErreur = "Chargement impossible : " + ex.Message;
        }

        OnPropertyChanged(nameof(SansIncident));
        OnPropertyChanged(nameof(SansAbandon));
        OnPropertyChanged(nameof(AbandonLegende));
    }
}