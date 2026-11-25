using System.Collections.ObjectModel;
using System.Globalization;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une ligne de la liste des paiements du jour
public class LignePaiementCaisse
{
    public string Heure { get; set; } = "";
    public string Ticket { get; set; } = "";
    public string Client { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Montant { get; set; } = "";
}

// Une ligne de la liste des commandes pas entièrement payées
public class LigneImpayeCaisse
{
    public string Ticket { get; set; } = "";
    public string Client { get; set; } = "";
    public string Depuis { get; set; } = "";
    public string Total { get; set; } = "";
    public string Paye { get; set; } = "";
    public string Reste { get; set; } = "";
}

// Propriétaire : Valdez
public class CaisseViewModel : ViewModelBase
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private DateTime _date = DateTime.Today;
    private bool _ongletImpayes;
    private string _messageErreur = "";
    private string _totalTexte = Fcfa(0);
    private string _especesTexte = Fcfa(0);
    private string _mobileMoneyTexte = Fcfa(0);
    private string _carteTexte = Fcfa(0);
    private string _nbPaiementsTexte = "0 paiement";
    private string _totalImpayesTexte = Fcfa(0);

    public ObservableCollection<LignePaiementCaisse> Paiements { get; } = new();
    public ObservableCollection<LigneImpayeCaisse> Impayes { get; } = new();

    public RelayCommand PrecedentCommand { get; }
    public RelayCommand SuivantCommand { get; }
    public RelayCommand AujourdhuiCommand { get; }

    public CaisseViewModel()
    {
        PrecedentCommand = new RelayCommand(_ => Date = Date.AddDays(-1));
        // On ne regarde pas dans le futur : il n'y a rien à encaisser demain
        SuivantCommand = new RelayCommand(_ => Date = Date.AddDays(1), _ => Date < DateTime.Today);
        AujourdhuiCommand = new RelayCommand(_ => Date = DateTime.Today, _ => Date != DateTime.Today);

        Charger();
    }

    // ===== Date =====

    public DateTime Date
    {
        get => _date;
        set
        {
            if (SetProperty(ref _date, value.Date))
            {
                OnPropertyChanged(nameof(DateTexte));
                Charger();
            }
        }
    }

    public string DateTexte
    {
        get
        {
            var texte = _date.ToString("dddd d MMMM yyyy", Fr);
            return char.ToUpper(texte[0], Fr) + texte.Substring(1);
        }
    }

    // ===== Onglets =====

    public bool OngletPaiements
    {
        get => !_ongletImpayes;
        set { if (value) ChoisirOnglet(false); }
    }

    public bool OngletImpayes
    {
        get => _ongletImpayes;
        set { if (value) ChoisirOnglet(true); }
    }

    private void ChoisirOnglet(bool impayes)
    {
        _ongletImpayes = impayes;
        OnPropertyChanged(nameof(OngletPaiements));
        OnPropertyChanged(nameof(OngletImpayes));
    }

    // ===== Chiffres =====

    public string TotalTexte { get => _totalTexte; private set => SetProperty(ref _totalTexte, value); }
    public string EspecesTexte { get => _especesTexte; private set => SetProperty(ref _especesTexte, value); }
    public string MobileMoneyTexte { get => _mobileMoneyTexte; private set => SetProperty(ref _mobileMoneyTexte, value); }
    public string CarteTexte { get => _carteTexte; private set => SetProperty(ref _carteTexte, value); }
    public string NbPaiementsTexte { get => _nbPaiementsTexte; private set => SetProperty(ref _nbPaiementsTexte, value); }
    public string TotalImpayesTexte { get => _totalImpayesTexte; private set => SetProperty(ref _totalImpayesTexte, value); }

    public bool SansPaiement => Paiements.Count == 0;
    public bool SansImpaye => Impayes.Count == 0;

    // ===== Message d'erreur =====

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

    // ===== Chargement =====

    private void Charger()
    {
        MessageErreur = "";

        // Un opérateur ne voit pas la caisse (règle 8.7), même si le menu la lui cache déjà
        if (Session.UtilisateurCourant?.Role == RoleUtilisateur.Operateur)
        {
            MessageErreur = "La caisse est réservée aux administrateurs et aux caissiers.";
            return;
        }

        try
        {
            // Le total et le détail par mode viennent de la même somme de paiements
            var resume = CaisseService.ObtenirResumeJournee(_date);
            TotalTexte = Fcfa(resume.Total);
            EspecesTexte = Fcfa(resume.TotalEspeces);
            MobileMoneyTexte = Fcfa(resume.TotalMobileMoney);
            CarteTexte = Fcfa(resume.TotalCarte);
            NbPaiementsTexte = resume.NbPaiements > 1
                ? $"{resume.NbPaiements} paiements"
                : $"{resume.NbPaiements} paiement";

            Paiements.Clear();
            foreach (var p in CaisseRepository.ObtenirDetailParJour(_date))
            {
                Paiements.Add(new LignePaiementCaisse
                {
                    Heure = p.DatePaiement.ToString("HH:mm", Fr),
                    Ticket = p.NumeroTicket,
                    Client = p.ClientNom,
                    Mode = LibelleMode(p.Mode),
                    Montant = Fcfa(p.Montant)
                });
            }

            // Les impayés ne dépendent pas de la date : ce sont toutes les commandes pas soldées
            Impayes.Clear();
            decimal totalImpayes = 0;
            foreach (var c in PaiementRepository.ObtenirCommandesImpayees())
            {
                totalImpayes += c.Reste;
                Impayes.Add(new LigneImpayeCaisse
                {
                    Ticket = c.NumeroTicket,
                    Client = c.ClientNom,
                    Depuis = c.DateDepot.ToString("dd/MM/yyyy", Fr),
                    Total = Fcfa(c.MontantTotal),
                    Paye = Fcfa(c.MontantPaye),
                    Reste = Fcfa(c.Reste)
                });
            }
            TotalImpayesTexte = Fcfa(totalImpayes);
        }
        catch (Exception ex)
        {
            // Une base injoignable ne doit jamais faire planter l'écran
            Paiements.Clear();
            Impayes.Clear();
            MessageErreur = "Chargement impossible : " + ex.Message;
        }

        OnPropertyChanged(nameof(SansPaiement));
        OnPropertyChanged(nameof(SansImpaye));
    }

       // Libellé lisible du mode de paiement
    private static string LibelleMode(ModePaiement mode) => mode switch
    {
        ModePaiement.Especes => "Espèces",
        ModePaiement.MobileMoney => "Mobile Money",
        ModePaiement.Carte => "Carte",
        _ => mode.ToString()
    };

    private static string Fcfa(decimal montant) => montant.ToString("N0", Fr) + " FCFA";
}