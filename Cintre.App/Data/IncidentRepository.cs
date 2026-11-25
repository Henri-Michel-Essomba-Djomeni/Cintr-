using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;

namespace Cintre.App.Data;

// Propriétaire : Valdez
public static class IncidentRepository
{
    // Colonnes d'un incident, avec le ticket et le nom du client (jointures)
    private const string SelectIncident =
        @"SELECT i.id, i.commande_id, i.type, i.description, i.date_incident,
                 i.resolu, i.resolution,
                 c.numero_ticket, cl.nom AS client_nom
            FROM incident i
            JOIN commande c ON c.id = i.commande_id
            JOIN client cl ON cl.id = c.client_id";

    // Enregistre un incident lié à une commande
    public static int Declarer(Incident i)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.ExecuteScalar<int>(
            @"INSERT INTO incident (commande_id, type, description)
              VALUES (@CommandeId, @Type, @Description)
              RETURNING id",
            new
            {
                i.CommandeId,
                Type = i.Type.ToString(),   // les enums sont stockés en texte
                i.Description
            });
    }

    // Les incidents non résolus d'abord, puis du plus récent au plus ancien
    public static List<Incident> ObtenirTous(bool seulementOuverts)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Incident>(
            SelectIncident + @"
             WHERE (@seulementOuverts = FALSE OR i.resolu = FALSE)
             ORDER BY i.resolu, i.date_incident DESC",
            new { seulementOuverts }).ToList();
    }

    // Pour le détail d'une commande
    public static List<Incident> ObtenirParCommande(int commandeId)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Incident>(
            SelectIncident + @"
             WHERE i.commande_id = @commandeId
             ORDER BY i.date_incident DESC",
            new { commandeId }).ToList();
    }

    // Marque l'incident résolu et garde l'explication
    public static void Resoudre(int id, string resolution)
    {
        using var cx = DbConnectionFactory.Creer();
        int lignes = cx.Execute(
            "UPDATE incident SET resolu = TRUE, resolution = @resolution WHERE id = @id",
            new { id, resolution });

        if (lignes == 0)
            throw new InvalidOperationException("Incident introuvable.");
    }

    // Commandes Prêtes dont la date prévue est plus vieille que "jours" jours (linge abandonné)
    public static List<Commande> ObtenirCommandesAbandonnees(int jours)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Commande>(
            @"SELECT c.id, c.numero_ticket, c.client_id, c.utilisateur_id, c.date_depot,
                     c.date_prevue, c.date_retrait, c.statut, c.urgent, c.remise_pourcent,
                     c.montant_total, c.montant_paye, c.remarque,
                     cl.nom AS client_nom
                FROM commande c
                JOIN client cl ON cl.id = c.client_id
               WHERE c.statut = 'Pret'
                 AND c.date_prevue < NOW() - make_interval(days => @jours)
               ORDER BY c.date_prevue",
            new { jours }).ToList();
    }
}