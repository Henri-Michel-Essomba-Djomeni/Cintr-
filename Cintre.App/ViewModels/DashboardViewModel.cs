using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une ligne de la liste « Commandes à traiter »
public record CommandeDashboard(string Ticket, string Client, StatutCommande Statut,
                                string Echeance, bool EnRetard, string Montant);

public class DashboardViewModel : ViewModelBase
{
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    private decimal _depotsDuJour;
    private decimal _retraitsDuJour;
    private decimal _recetteDuJour;
    private decimal _enRetard;
    private bool _donneesDemo;
    private bool _aucuneCommande;

    public DashboardViewModel()
    {
        CommandeNouveauDepot = new RelayCommand(_ => NavigationService.Naviguer("Depot"));
        CommandeRetrait = new RelayCommand(_ => NavigationService.Naviguer("Retrait"));
    }

    public ICommand CommandeNouveauDepot { get; }
    public ICommand CommandeRetrait { get; }

    public ObservableCollection<CommandeDashboard> Commandes { get; } = new();

    // Les compteurs sont des decimal : c'est le type attendu par CountUpText
    public decimal DepotsDuJour { get => _depotsDuJour; private set => SetProperty(ref _depotsDuJour, value); }
    public decimal RetraitsDuJour { get => _retraitsDuJour; private set => SetProperty(ref _retraitsDuJour, value); }
    public decimal RecetteDuJour { get => _recetteDuJour; private set => SetProperty(ref _recetteDuJour, value); }

    public decimal EnRetard
    {
        get => _enRetard;
        private set
        {
            if (SetProperty(ref _enRetard, value))
                OnPropertyChanged(nameof(ARetard));
        }
    }

    public bool ARetard => EnRetard > 0;

    public bool DonneesDemo { get => _donneesDemo; private set => SetProperty(ref _donneesDemo, value); }

    public bool AucuneCommande { get => _aucuneCommande; private set => SetProperty(ref _aucuneCommande, value); }

    // Un Opérateur ne voit pas la recette (fiche A4)
    public bool AfficherRecette => Session.UtilisateurCourant?.Role != RoleUtilisateur.Operateur;

    public string Salutation
    {
        get
        {
            var nom = Session.UtilisateurCourant?.Nom;
            return string.IsNullOrWhiteSpace(nom) ? "Bonjour" : "Bonjour, " + nom.Trim().Split(' ')[0];
        }
    }

    public string DateTexte
    {
        get
        {
            var texte = DateTime.Now.ToString("dddd d MMMM yyyy", Francais);
            return char.ToUpper(texte[0], Francais) + texte[1..];
        }
    }

    public async Task ChargerAsync()
    {
        bool demo = false;

        ResumeJour resume;
        try
        {
            resume = await Task.Run(() => StatsService.ObtenirResumeDuJour());
        }
        catch (NotImplementedException)
        {
            resume = ResumeDemo();
            demo = true;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les chiffres du jour : " + ex.Message);
            resume = new ResumeJour();
        }

        List<Commande> commandes;
        try
        {
            commandes = await Task.Run(() => CommandeRepository.Rechercher(string.Empty, null));
        }
        catch (NotImplementedException)
        {
            commandes = CommandesDemo();
            demo = true;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les commandes : " + ex.Message);
            commandes = new();
        }

        DepotsDuJour = resume.NbDepots;
        RetraitsDuJour = resume.NbRetraits;
        RecetteDuJour = resume.RecetteJour;
        EnRetard = resume.NbEnRetard;

        // Les 5 commandes non terminées les plus anciennes
        var aTraiter = commandes
            .Where(c => c.Statut is StatutCommande.Recu or StatutCommande.EnTraitement or StatutCommande.Pret)
            .OrderBy(c => c.DateDepot)
            .Take(5);

        Commandes.Clear();
        foreach (var c in aTraiter)
            Commandes.Add(VersLigne(c));

        DonneesDemo = demo;
        AucuneCommande = Commandes.Count == 0;
    }

    private static CommandeDashboard VersLigne(Commande c)
    {
        var echeance = c.DatePrevue.ToString("ddd d MMM", Francais);
        if (c.EnRetard) echeance += " · en retard";

        return new CommandeDashboard(
            c.NumeroTicket,
            string.IsNullOrWhiteSpace(c.ClientNom) ? $"Client n°{c.ClientId}" : c.ClientNom,
            c.Statut,
            echeance,
            c.EnRetard,
            c.MontantTotal.ToString("N0", Francais) + " " + ParametresService.Devise);
    }

    // ----- Données fictives, utilisées uniquement tant que les services ne sont pas branchés -----

    private static ResumeJour ResumeDemo() => new()
    {
        NbDepots = 7,
        NbRetraits = 4,
        RecetteJour = 185000,
        NbEnRetard = 2,
        NbPretsNonRetires = 3,
        TotalImpayes = 42000
    };

    private static List<Commande> CommandesDemo()
    {
        var maintenant = DateTime.Now;
        return new List<Commande>
        {
            new() { NumeroTicket = "CT-20261124-0003", ClientNom = "Amina Nkoulou", Statut = StatutCommande.Pret,
                    DateDepot = maintenant.AddDays(-3), DatePrevue = maintenant.AddDays(-1), MontantTotal = 12500 },
            new() { NumeroTicket = "CT-20261125-0001", ClientNom = "Jean-Paul Mbarga", Statut = StatutCommande.EnTraitement,
                    DateDepot = maintenant.AddDays(-2), DatePrevue = maintenant.AddHours(-5), MontantTotal = 8000 },
            new() { NumeroTicket = "CT-20261125-0004", ClientNom = "Sandrine Fotso", Statut = StatutCommande.Recu,
                    DateDepot = maintenant.AddDays(-1), DatePrevue = maintenant.AddDays(2), MontantTotal = 21000 },
            new() { NumeroTicket = "CT-20261126-0001", ClientNom = "Paul Etoundi", Statut = StatutCommande.EnTraitement,
                    DateDepot = maintenant.AddHours(-6), DatePrevue = maintenant.AddDays(3), MontantTotal = 4500 },
            new() { NumeroTicket = "CT-20261126-0002", ClientNom = "Carine Tchoumi", Statut = StatutCommande.Recu,
                    DateDepot = maintenant.AddHours(-2), DatePrevue = maintenant.AddDays(3), MontantTotal = 9500 }
        };
    }
}