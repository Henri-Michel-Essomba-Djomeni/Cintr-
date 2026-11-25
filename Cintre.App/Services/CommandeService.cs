using Cintre.App.Data;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Hugo
// Règles métier des commandes (sections 8.1 à 8.4 du dossier de conception).
public static class CommandeService
{
    // Règle 8.1 : numéro de ticket au format PR-AAAAMMJJ-NNNN.
    // NNNN = nombre de commandes déjà créées aujourd'hui + 1.
    public static string GenererNumeroTicket()
    {
        DateTime maintenant = DateTime.Now;

        // on compte les commandes du jour dans la base (le SQL reste dans le Repository)
        int numeroDuJour = CommandeRepository.CompterDuJour(maintenant) + 1;

        return $"PR-{maintenant:yyyyMMdd}-{numeroDuJour:D4}";
    }

    // Règle 8.2 : prix d'une commande.
    public static decimal CalculerTotal(List<LigneCommande> lignes, bool urgent, decimal remisePourcent)
    {
        // brut = somme des sous-totaux (quantité x prix unitaire copié dans la ligne)
        decimal brut = lignes.Sum(l => l.SousTotal);

        // supplément du service urgent
        if (urgent)
        {
            brut = brut * (1 + ParametresService.SupplementUrgencePourcent / 100);
        }

        // remise (fidélité ou saisie par un Admin)
        decimal total = brut * (1 - remisePourcent / 100);

        // arrondi à l'entier, car le FCFA n'a pas de décimales
        return Math.Round(total, 0, MidpointRounding.AwayFromZero);
    }

    // Règle 8.3 : date prévue de retrait.
    public static DateTime CalculerDatePrevue(DateTime depot, bool urgent)
    {
        // urgent = le lendemain, sinon le délai standard des paramètres
        return urgent
            ? depot.AddDays(1)
            : depot.AddDays(ParametresService.DelaiStandardJours);
    }

    // Règle 8.4 : seules ces transitions sont autorisées.
    // Attention : la vérification du solde pour passer à Retire est faite par
    // RetraitService (Valdez), pas ici. L'écran Commandes ne propose pas Retire.
    public static bool TransitionAutorisee(StatutCommande actuel, StatutCommande nouveau, bool estAdmin)
    {
        return (actuel, nouveau) switch
        {
            // le cycle normal
            (StatutCommande.Recu, StatutCommande.EnTraitement) => true,
            (StatutCommande.EnTraitement, StatutCommande.Pret) => true,
            (StatutCommande.Pret, StatutCommande.Retire) => true,

            // annulation possible tant que le linge n'est pas rendu
            (StatutCommande.Recu or StatutCommande.EnTraitement or StatutCommande.Pret,
             StatutCommande.Annule) => true,

            // seul retour en arrière : un Admin renvoie un linge Prêt en relavage
            (StatutCommande.Pret, StatutCommande.EnTraitement) => estAdmin,

            // tout le reste est refusé (ex. Reçu vers Retiré)
            _ => false
        };
    }

    // Règle d'annulation (8.4). Lève une exception avec un message clair
    // quand c'est refusé : le ViewModel l'affiche avec MessageService.Erreur.
    public static void Annuler(int commandeId, int utilisateurId)
    {
        Commande? commande = CommandeRepository.ObtenirParId(commandeId);

        if (commande == null)
            throw new InvalidOperationException("Commande introuvable.");

        if (commande.Statut == StatutCommande.Retire)
            throw new InvalidOperationException("Une commande déjà retirée ne peut plus être annulée.");

        if (commande.Statut == StatutCommande.Annule)
            throw new InvalidOperationException("Cette commande est déjà annulée.");

        // s'il y a déjà des paiements, seul un Admin peut annuler
        if (commande.MontantPaye > 0 && !Session.EstAdmin)
            throw new InvalidOperationException(
                "Cette commande a déjà des paiements : seul un administrateur peut l'annuler.");

        CommandeRepository.ChangerStatut(commandeId, StatutCommande.Annule, utilisateurId);
    }
}
