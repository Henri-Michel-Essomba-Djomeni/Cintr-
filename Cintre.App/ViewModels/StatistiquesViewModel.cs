using System.Globalization;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Propriétaire : Raïssa
public class StatistiquesViewModel : ViewModelBase
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private const int JoursMax = 92; // au-delà, le graphique serait illisible

    private string _periode = "7";   // "7", "30" ou "perso"
    private DateTime _debut = DateTime.Today.AddDays(-6);
    private DateTime _fin = DateTime.Today;

    private ResumeJour _resume = new();
    private List<PointStat> _recettes = new();
    private List<PointStat> _topServices = new();
    private List<PointStat> _topClients = new();
    private List<PointStat> _statuts = new();
    private decimal _totalPeriode;
    private string _textePeriode = "";

    public RelayCommand RafraichirCommand { get; }

    public StatistiquesViewModel()
    {
        RafraichirCommand = new RelayCommand(_ => Charger());
        Charger();
    }

    // ---------- Chiffres du jour ----------
    public decimal DepotsJour => _resume.NbDepots;
    public decimal RetraitsJour => _resume.NbRetraits;
    public decimal RecetteJour => _resume.RecetteJour;
    public decimal EnRetard => _resume.NbEnRetard;
    public decimal PretsNonRetires => _resume.NbPretsNonRetires;
    public decimal Impayes => _resume.TotalImpayes;

    // ---------- Graphique et classements ----------
    public List<PointStat> Recettes { get => _recettes; private set => SetProperty(ref _recettes, value); }
    public List<PointStat> TopServices { get => _topServices; private set => SetProperty(ref _topServices, value); }
    public List<PointStat> TopClients { get => _topClients; private set => SetProperty(ref _topClients, value); }
    public List<PointStat> Statuts { get => _statuts; private set => SetProperty(ref _statuts, value); }

    public decimal TotalPeriode { get => _totalPeriode; private set => SetProperty(ref _totalPeriode, value); }
    public string TexteTotal => $"Total : {_totalPeriode.ToString("N0", Fr)} FCFA";
    public string TextePeriode { get => _textePeriode; private set => SetProperty(ref _textePeriode, value); }

    // ---------- Sélecteur de période ----------
    public bool Est7Jours
    {
        get => _periode == "7";
        set { if (value) ChoisirPeriode("7"); }
    }

    public bool Est30Jours
    {
        get => _periode == "30";
        set { if (value) ChoisirPeriode("30"); }
    }

    public bool EstPerso
    {
        get => _periode == "perso";
        set { if (value) ChoisirPeriode("perso"); }
    }

    public DateTime? DateDebut
    {
        get => _debut;
        set
        {
            if (value is { } d && d.Date != _debut.Date)
            {
                _debut = d.Date;
                OnPropertyChanged(nameof(DateDebut));
                if (EstPerso) ChargerGraphique();
            }
        }
    }

    public DateTime? DateFin
    {
        get => _fin;
        set
        {
            if (value is { } d && d.Date != _fin.Date)
            {
                _fin = d.Date;
                OnPropertyChanged(nameof(DateFin));
                if (EstPerso) ChargerGraphique();
            }
        }
    }

    private void ChoisirPeriode(string periode)
    {
        if (_periode == periode)
            return;
        _periode = periode;
        OnPropertyChanged(nameof(Est7Jours));
        OnPropertyChanged(nameof(Est30Jours));
        OnPropertyChanged(nameof(EstPerso));
        ChargerGraphique();
    }

    // Début et fin de la période choisie
    private (DateTime Debut, DateTime Fin) PeriodeCourante()
    {
        var aujourdhui = DateTime.Today;
        switch (_periode)
        {
            case "30":
                return (aujourdhui.AddDays(-29), aujourdhui);
            case "perso":
                var debut = _debut.Date;
                var fin = _fin.Date;
                if (debut > fin) (debut, fin) = (fin, debut);
                if ((fin - debut).TotalDays > JoursMax)
                {
                    debut = fin.AddDays(-JoursMax);
                    MessageService.Info($"La période est limitée à {JoursMax} jours.");
                }
                return (debut, fin);
            default:
                return (aujourdhui.AddDays(-6), aujourdhui);
        }
    }

    // ---------- Chargement ----------
    private void Charger()
    {
        try
        {
            _resume = StatsService.ObtenirResumeDuJour();
            TopServices = StatsService.ObtenirTopServices(5);
            TopClients = StatsService.ObtenirTopClients(5);
            Statuts = StatsService.ObtenirRepartitionStatuts()
                                  .Select(p => new PointStat { Libelle = LibelleStatut(p.Libelle), Valeur = p.Valeur })
                                  .ToList();
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les statistiques : " + ex.Message);
        }

        OnPropertyChanged(nameof(DepotsJour));
        OnPropertyChanged(nameof(RetraitsJour));
        OnPropertyChanged(nameof(RecetteJour));
        OnPropertyChanged(nameof(EnRetard));
        OnPropertyChanged(nameof(PretsNonRetires));
        OnPropertyChanged(nameof(Impayes));

        ChargerGraphique();
    }

    // Seul le graphique est rechargé quand on change de période
    private void ChargerGraphique()
    {
        try
        {
            var (debut, fin) = PeriodeCourante();
            Recettes = StatsService.ObtenirRecettesParJour(debut, fin);
            TotalPeriode = Recettes.Sum(p => p.Valeur);
            TextePeriode = $"Du {debut:dd/MM/yyyy} au {fin:dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les recettes : " + ex.Message);
        }
        OnPropertyChanged(nameof(TexteTotal));
    }

    private static string LibelleStatut(string statut) => statut switch
    {
        "Recu" => "Reçu",
        "EnTraitement" => "En traitement",
        "Pret" => "Prêt",
        "Retire" => "Retiré",
        "Annule" => "Annulé",
        _ => statut
    };
}
