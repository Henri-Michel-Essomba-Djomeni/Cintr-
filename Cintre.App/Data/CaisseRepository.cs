using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;

namespace Cintre.App.Data;

// Une ligne du détail de la caisse : un paiement avec son ticket et son client
public class PaiementJour
{
    public DateTime DatePaiement { get; set; }
    public string NumeroTicket { get; set; } = "";
    public string ClientNom { get; set; } = "";
    public ModePaiement Mode { get; set; }
    public decimal Montant { get; set; }
}

// Propriétaire : Valdez
public static class CaisseRepository
{
    // Paiements d'une journée avec ticket et nom du client, pour la liste de la caisse.
    // Même intervalle de dates que PaiementRepository.ObtenirParJour : les totaux concordent.
    public static List<PaiementJour> ObtenirDetailParJour(DateTime jour)
    {
        using var cx = DbConnectionFactory.Creer();
        var debut = jour.Date;
        var fin = debut.AddDays(1);
        return cx.Query<PaiementJour>(
            @"SELECT p.date_paiement, c.numero_ticket, cl.nom AS client_nom, p.mode, p.montant
                FROM paiement p
                JOIN commande c ON c.id = p.commande_id
                JOIN client cl ON cl.id = c.client_id
               WHERE p.date_paiement >= @debut AND p.date_paiement < @fin
               ORDER BY p.date_paiement, p.id",
            new { debut, fin }).ToList();
    }
}