using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;

namespace Cintre.App.Data;

// Propriétaire : Valdez
public static class PaiementRepository
{
    // Colonnes communes : les noms snake_case sont reliés automatiquement aux propriétés C#
    private const string ColonnesPaiement =
        "id, commande_id, montant, mode, date_paiement, utilisateur_id";

    // Enregistre un paiement ET met à jour montant_paye dans la MÊME transaction.
    // Si l'une des deux écritures échoue, rien n'est enregistré : la caisse reste juste.
    public static int Enregistrer(Paiement p)
    {
        using var cx = DbConnectionFactory.Creer();
        using var tx = cx.BeginTransaction();
        try
        {
            int id = cx.ExecuteScalar<int>(
                @"INSERT INTO paiement (commande_id, montant, mode, utilisateur_id)
                  VALUES (@CommandeId, @Montant, @Mode, @UtilisateurId)
                  RETURNING id",
                new
                {
                    p.CommandeId,
                    p.Montant,
                    Mode = p.Mode.ToString(),   // les enums sont stockés en texte
                    p.UtilisateurId
                }, tx);

            // La condition protège contre deux encaissements simultanés qui dépasseraient le total
            int lignes = cx.Execute(
                @"UPDATE commande
                     SET montant_paye = montant_paye + @Montant
                   WHERE id = @CommandeId
                     AND montant_paye + @Montant <= montant_total",
                new { p.Montant, p.CommandeId }, tx);

            if (lignes != 1)
                throw new InvalidOperationException(
                    "Le paiement dépasse le reste à payer (la commande a peut-être changé).");

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // Du plus ancien au plus récent
    public static List<Paiement> ObtenirParCommande(int commandeId)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Paiement>(
            $@"SELECT {ColonnesPaiement} FROM paiement
               WHERE commande_id = @commandeId
               ORDER BY date_paiement, id",
            new { commandeId }).ToList();
    }

    // Tous les paiements d'une journée (de 00h00 inclus à 00h00 du lendemain exclu)
    public static List<Paiement> ObtenirParJour(DateTime jour)
    {
        using var cx = DbConnectionFactory.Creer();
        var debut = jour.Date;
        var fin = debut.AddDays(1);
        return cx.Query<Paiement>(
            $@"SELECT {ColonnesPaiement} FROM paiement
               WHERE date_paiement >= @debut AND date_paiement < @fin
               ORDER BY date_paiement, id",
            new { debut, fin }).ToList();
    }

    // Commandes pas entièrement payées, hors commandes annulées
    public static List<Commande> ObtenirCommandesImpayees()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Commande>(
            @"SELECT c.id, c.numero_ticket, c.client_id, c.utilisateur_id, c.date_depot,
                     c.date_prevue, c.date_retrait, c.statut, c.urgent, c.remise_pourcent,
                     c.montant_total, c.montant_paye, c.remarque,
                     cl.nom AS client_nom
                FROM commande c
                JOIN client cl ON cl.id = c.client_id
               WHERE c.montant_paye < c.montant_total
                 AND c.statut <> 'Annule'
               ORDER BY c.date_depot").ToList();
    }
    
    // Enregistre l'heure de retrait seulement si elle est vide (évite d'écraser une date déjà posée)
    public static void AssurerDateRetrait(int commandeId)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute(
            "UPDATE commande SET date_retrait = NOW() WHERE id = @commandeId AND date_retrait IS NULL",
            new { commandeId });
    }
}