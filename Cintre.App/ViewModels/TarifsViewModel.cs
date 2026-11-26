using System.Collections.ObjectModel;
using System.Globalization;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une case de la grille : le prix d'un service pour un type de vêtement (null = non proposé)
public class CelluleTarif : ViewModelBase
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private decimal? _prix;

    public int ServiceId { get; init; }
    public int TypeVetementId { get; init; }
    public string ServiceNom { get; init; } = "";
    public string TypeNom { get; init; } = "";
    public decimal? PrixOriginal { get; init; }

    public decimal? Prix
    {
        get => _prix;
        set
        {
            if (SetProperty(ref _prix, value))
            {
                OnPropertyChanged(nameof(Texte));
                OnPropertyChanged(nameof(Modifiee));
            }
        }
    }

    public bool Modifiee => _prix != PrixOriginal;
    public string Texte => _prix.HasValue ? _prix.Value.ToString("N0", Fr) : "—";
}

// Une ligne de la grille : un type de vêtement et ses cases (une par service actif)
public class LigneGrille
{
    public LigneGrille(string typeNom, List<CelluleTarif> cellules)
    {
        TypeNom = typeNom;
        Cellules = cellules;
    }

    public string TypeNom { get; }
    public List<CelluleTarif> Cellules { get; }
}

// Propriétaire : Raïssa
public class TarifsViewModel : ViewModelBase
{
    // ---------- Onglet Services ----------
    private Service? _serviceSelection;
    private int _serviceId;
    private string _titreService = "Nouveau service";
    private string _serviceNom = "", _serviceDescription = "", _serviceErreur = "";
    private bool _serviceActif = true;

    // ---------- Onglet Types ----------
    private string _typeNom = "", _typeErreur = "";

    // ---------- Grille ----------
    private int _nbModifications;
    private bool _editionOuverte;
    private CelluleTarif? _celluleEnEdition;
    private string _titreEdition = "", _saisiePrix = "", _erreurSaisie = "";

    public ObservableCollection<Service> Services { get; } = new();
    public ObservableCollection<TypeVetement> Types { get; } = new();
    public ObservableCollection<Service> Colonnes { get; } = new();
    public ObservableCollection<LigneGrille> Lignes { get; } = new();

    public RelayCommand NouveauServiceCommand { get; }
    public RelayCommand EnregistrerServiceCommand { get; }
    public RelayCommand AjouterTypeCommand { get; }
    public RelayCommand EnregistrerGrilleCommand { get; }
    public RelayCommand EditerCelluleCommand { get; }
    public RelayCommand ValiderPrixCommand { get; }
    public RelayCommand RetirerPrixCommand { get; }

    public TarifsViewModel()
    {
        NouveauServiceCommand = new RelayCommand(_ => NouveauService());
        EnregistrerServiceCommand = new RelayCommand(_ => EnregistrerService());
        AjouterTypeCommand = new RelayCommand(_ => AjouterType());
        EnregistrerGrilleCommand = new RelayCommand(_ => EnregistrerGrille(), _ => NbModifications > 0);
        EditerCelluleCommand = new RelayCommand(p => EditerCellule(p));
        ValiderPrixCommand = new RelayCommand(_ => ValiderPrix());
        RetirerPrixCommand = new RelayCommand(_ => RetirerPrix());

        Charger();
    }

    // =========================================================
    // Services
    // =========================================================
    public Service? ServiceSelection
    {
        get => _serviceSelection;
        set
        {
            if (SetProperty(ref _serviceSelection, value) && value != null)
                ChargerFormulaireService(value);
        }
    }

    public string TitreService { get => _titreService; private set => SetProperty(ref _titreService, value); }

    public string ServiceNom
    {
        get => _serviceNom;
        set { if (SetProperty(ref _serviceNom, value)) ServiceErreur = ""; }
    }

    public string ServiceDescription { get => _serviceDescription; set => SetProperty(ref _serviceDescription, value); }
    public bool ServiceActif { get => _serviceActif; set => SetProperty(ref _serviceActif, value); }
    public string ServiceErreur { get => _serviceErreur; set => SetProperty(ref _serviceErreur, value); }

    private void NouveauService()
    {
        _serviceId = 0;
        _serviceSelection = null;
        OnPropertyChanged(nameof(ServiceSelection));
        ServiceNom = "";
        ServiceDescription = "";
        ServiceActif = true;
        ServiceErreur = "";
        TitreService = "Nouveau service";
    }

    private void ChargerFormulaireService(Service s)
    {
        _serviceId = s.Id;
        ServiceNom = s.Nom;
        ServiceDescription = s.Description ?? "";
        ServiceActif = s.Actif;
        ServiceErreur = "";
        TitreService = s.Nom;
    }

    private void EnregistrerService()
    {
        ServiceErreur = string.IsNullOrWhiteSpace(ServiceNom) ? "Le nom du service est obligatoire." : "";
        if (ServiceErreur != "")
            return;

        var service = new Service
        {
            Id = _serviceId,
            Nom = ServiceNom.Trim(),
            Description = string.IsNullOrWhiteSpace(ServiceDescription) ? null : ServiceDescription.Trim(),
            Actif = ServiceActif
        };

        try
        {
            if (_serviceId == 0)
            {
                _serviceId = TarifRepository.AjouterService(service);
                MessageService.Succes("Service ajouté.");
            }
            else
            {
                TarifRepository.ModifierService(service);
                if (service.Actif)
                    MessageService.Succes("Service modifié.");
                else
                    MessageService.Info("Service désactivé : il n'apparaît plus au Dépôt.");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Nom déjà utilisé
            ServiceErreur = ex.Message;
            return;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible d'enregistrer le service : " + ex.Message);
            return;
        }

        Charger();
        var courant = Services.FirstOrDefault(x => x.Id == _serviceId);
        if (courant != null)
            ChargerFormulaireService(courant);
    }

    // =========================================================
    // Types de vêtements
    // =========================================================
    public string TypeNom
    {
        get => _typeNom;
        set { if (SetProperty(ref _typeNom, value)) TypeErreur = ""; }
    }

    public string TypeErreur { get => _typeErreur; set => SetProperty(ref _typeErreur, value); }

    private void AjouterType()
    {
        TypeErreur = string.IsNullOrWhiteSpace(TypeNom) ? "Le nom du type est obligatoire." : "";
        if (TypeErreur != "")
            return;

        try
        {
            TarifRepository.AjouterTypeVetement(new TypeVetement { Nom = TypeNom.Trim() });
        }
        catch (InvalidOperationException ex)
        {
            TypeErreur = ex.Message;
            return;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible d'ajouter le type : " + ex.Message);
            return;
        }

        TypeNom = "";
        MessageService.Succes("Type ajouté.");
        Charger();
    }

    // =========================================================
    // Grille des prix
    // =========================================================
    public int NbModifications { get => _nbModifications; private set => SetProperty(ref _nbModifications, value); }

    public string TexteModifications =>
        NbModifications == 0 ? "Aucune modification" : $"{NbModifications} modification(s) non enregistrée(s)";

    public bool EditionOuverte { get => _editionOuverte; set => SetProperty(ref _editionOuverte, value); }
    public string TitreEdition { get => _titreEdition; private set => SetProperty(ref _titreEdition, value); }
    public string ErreurSaisie { get => _erreurSaisie; private set => SetProperty(ref _erreurSaisie, value); }

    public string SaisiePrix
    {
        get => _saisiePrix;
        set { if (SetProperty(ref _saisiePrix, value)) ErreurSaisie = ""; }
    }

    private void EditerCellule(object? parametre)
    {
        if (parametre is not CelluleTarif cellule)
            return;

        _celluleEnEdition = cellule;
        TitreEdition = $"{cellule.TypeNom} · {cellule.ServiceNom}";
        SaisiePrix = cellule.Prix.HasValue ? cellule.Prix.Value.ToString("0", CultureInfo.InvariantCulture) : "";
        ErreurSaisie = "";
        EditionOuverte = true;
    }

    private void ValiderPrix()
    {
        if (_celluleEnEdition == null)
            return;

        // On ignore les espaces (1 500) et on accepte la virgule
        var texte = new string(SaisiePrix.Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(',', '.');
        if (texte == "")
        {
            ErreurSaisie = "Saisissez un prix, ou cliquez sur Retirer.";
            return;
        }
        if (!decimal.TryParse(texte, NumberStyles.Number, CultureInfo.InvariantCulture, out var prix))
        {
            ErreurSaisie = "Prix invalide.";
            return;
        }
        if (prix < 0)
        {
            ErreurSaisie = "Le prix ne peut pas être négatif.";
            return;
        }

        _celluleEnEdition.Prix = Math.Round(prix, 0);
        EditionOuverte = false;
    }

    private void RetirerPrix()
    {
        if (_celluleEnEdition == null)
            return;
        _celluleEnEdition.Prix = null;
        EditionOuverte = false;
    }

    private void EnregistrerGrille()
    {
        var n = 0;
        try
        {
            foreach (var c in Lignes.SelectMany(l => l.Cellules).Where(c => c.Modifiee).ToList())
            {
                if (c.Prix.HasValue)
                    TarifRepository.DefinirPrix(c.ServiceId, c.TypeVetementId, c.Prix.Value);
                else
                    TarifRepository.RetirerPrix(c.ServiceId, c.TypeVetementId);
                n++;
            }
            MessageService.Succes($"{n} prix enregistré(s).");
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible d'enregistrer les prix : " + ex.Message);
        }
        Charger();
    }

    private void MajModifications()
    {
        NbModifications = Lignes.SelectMany(l => l.Cellules).Count(c => c.Modifiee);
        OnPropertyChanged(nameof(TexteModifications));
    }

    // =========================================================
    // Chargement (tout recharger d'un coup, en gardant les prix pas encore enregistrés)
    // =========================================================
    private void Charger()
    {
        try
        {
            var services = TarifRepository.ObtenirServices();
            var types = TarifRepository.ObtenirTypesVetement();
            var prix = TarifRepository.ObtenirTarifs()
                                      .ToDictionary(t => (t.ServiceId, t.TypeVetementId), t => t.Prix);
            var enAttente = Lignes.SelectMany(l => l.Cellules)
                                  .Where(c => c.Modifiee)
                                  .ToDictionary(c => (c.ServiceId, c.TypeVetementId), c => c.Prix);

            Services.Clear();
            foreach (var s in services) Services.Add(s);

            Types.Clear();
            foreach (var t in types) Types.Add(t);

            // La grille ne montre que les services actifs
            Colonnes.Clear();
            foreach (var s in services.Where(s => s.Actif)) Colonnes.Add(s);

            Lignes.Clear();
            foreach (var t in types)
            {
                var cellules = new List<CelluleTarif>();
                foreach (var s in Colonnes)
                {
                    decimal? p = prix.TryGetValue((s.Id, t.Id), out var v) ? v : null;
                    var cellule = new CelluleTarif
                    {
                        ServiceId = s.Id,
                        TypeVetementId = t.Id,
                        ServiceNom = s.Nom,
                        TypeNom = t.Nom,
                        PrixOriginal = p,
                        Prix = p
                    };
                    if (enAttente.TryGetValue((s.Id, t.Id), out var attente))
                        cellule.Prix = attente;
                    cellule.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(CelluleTarif.Prix)) MajModifications();
                    };
                    cellules.Add(cellule);
                }
                Lignes.Add(new LigneGrille(t.Nom, cellules));
            }
            MajModifications();
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les tarifs : " + ex.Message);
        }

        // On garde le service en cours de modification, s'il existe encore
        _serviceSelection = _serviceId == 0 ? null : Services.FirstOrDefault(s => s.Id == _serviceId);
        OnPropertyChanged(nameof(ServiceSelection));
    }
}
