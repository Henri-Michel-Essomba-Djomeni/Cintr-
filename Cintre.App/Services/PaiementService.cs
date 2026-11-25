using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Valdez
public static class PaiementService
{
    // Règles 8.5 : un paiement est strictement positif et ne dépasse pas le reste à payer
    public static (bool ok, string message) Valider(Commande c, decimal montant)
    {
        if (c == null)
            return (false, "Aucune commande sélectionnée.");

        if (c.Statut == StatutCommande.Annule)
            return (false, "Cette commande est annulée : aucun paiement possible.");

        if (montant <= 0)
            return (false, "Le montant doit être supérieur à 0.");

        // Les montants sont en FCFA, sans décimales
        if (montant != Math.Round(montant, 0))
            return (false, "Le montant doit être un nombre entier de FCFA.");

        if (c.Reste <= 0)
            return (false, "Cette commande est déjà entièrement payée.");

        if (montant > c.Reste)
            return (false, $"Le montant dépasse le reste à payer ({c.Reste:N0} FCFA).");

        return (true, "Paiement valide.");
    }
}