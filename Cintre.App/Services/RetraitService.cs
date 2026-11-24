using Cintre.App.Data;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Valdez
public static class RetraitService
{
    // Rend le linge au client. Règles 8.4 : statut Prêt ET solde à zéro.
    public static (bool ok, string message) Retirer(string ticket, int utilisateurId)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return (false, "Saisis un numéro de ticket.");

        ticket = ticket.Trim().ToUpperInvariant();

        try
        {
            var c = CommandeRepository.ObtenirParTicket(ticket);

            // Un ticket mal saisi donne un message, pas un plantage
            if (c == null)
                return (false, $"Aucune commande trouvée pour le ticket {ticket}.");

            if (c.Statut == StatutCommande.Annule)
                return (false, "Cette commande est annulée.");

            if (c.Statut == StatutCommande.Retire)
            {
                string quand = c.DateRetrait?.ToString("dd/MM/yyyy 'à' HH:mm") ?? "une date inconnue";
                return (false, $"Déjà retiré le {quand}.");
            }

            if (c.Statut != StatutCommande.Pret)
                return (false, "Linge pas encore prêt.");

            // On vérifie le solde AVANT de rendre le linge
            if (c.Reste > 0)
                return (false, $"Il reste {c.Reste:N0} FCFA à payer.");

            // Le changement de statut est fait par le repository de Hugo (historique inclus)
            CommandeRepository.ChangerStatut(c.Id, StatutCommande.Retire, utilisateurId);
            PaiementRepository.AssurerDateRetrait(c.Id);

            // 1 point par tranche de 1000 FCFA, crédité au retrait (règle 8.6)
            int points = FideliteService.CalculerPoints(c.MontantTotal);
            if (points > 0)
                ClientRepository.AjouterPoints(c.ClientId, points);

            return (true, points > 0
                ? $"Linge remis au client. {points} point(s) de fidélité ajouté(s)."
                : "Linge remis au client.");
        }
        catch (Exception ex)
        {
            return (false, "Retrait impossible : " + ex.Message);
        }
    }
}