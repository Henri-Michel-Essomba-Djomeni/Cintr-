using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;
using Npgsql;

namespace Cintre.App.Data;

// Propriétaire : Hugo
// Tout le SQL des commandes est ici (règle : seuls les Repositories parlent à la base).
public static class CommandeRepository
{
    // Requête de base : une commande avec le nom de son client.
    // Les colonnes snake_case sont reliées aux propriétés PascalCase par Dapper.
    private const string SqlBase =
        @"SELECT c.id, c.numero_ticket, c.client_id, c.utilisateur_id, c.date_depot,
                 c.date_prevue, c.date_retrait, c.statut, c.urgent, c.remise_pourcent,
                 c.montant_total, c.montant_paye, c.remarque, cl.nom AS client_nom
          FROM commande c
          JOIN client cl ON cl.id = c.client_id";

    // Les colonnes sont de type « timestamp » (sans fuseau) : on envoie des dates sans Kind
    // pour éviter toute erreur de conversion avec Npgsql.
    private static DateTime Naif(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Unspecified);

    // ------------------------------------------------------------------
    // Création (règle 8.2 : tout dans UNE transaction)
    // ------------------------------------------------------------------

    // Insère la commande, toutes ses lignes et la première ligne d'historique.
    // Si le numéro de ticket existe déjà (deux dépôts en même temps), on le recalcule une fois.
    public static int Creer(Commande c)
    {
        if (c.ClientId <= 0)
            throw new InvalidOperationException("Une commande doit avoir un client.");
        if (c.Lignes == null || c.Lignes.Count == 0)
            throw new InvalidOperationException("Une commande doit avoir au moins une ligne.");

        if (string.IsNullOrWhiteSpace(c.NumeroTicket))
            c.NumeroTicket = CommandeService.GenererNumeroTicket();

        try
        {
            return CreerUneFois(c);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // doublon de ticket : la transaction a été annulée, on recalcule et on réessaie
            c.NumeroTicket = CommandeService.GenererNumeroTicket();
            return CreerUneFois(c);
        }
    }

    private static int CreerUneFois(Commande c)
    {
        using var cx = DbConnectionFactory.Creer();
        // Commit seulement à la toute fin : en cas d'erreur, rien n'est enregistré
        using var tx = cx.BeginTransaction();

        int id = cx.ExecuteScalar<int>(
            @"INSERT INTO commande
                (numero_ticket, client_id, utilisateur_id, date_depot, date_prevue,
                 statut, urgent, remise_pourcent, montant_total, montant_paye, remarque)
              VALUES
                (@NumeroTicket, @ClientId, @UtilisateurId, @DateDepot, @DatePrevue,
                 @Statut, @Urgent, @RemisePourcent, @MontantTotal, @MontantPaye, @Remarque)
              RETURNING id",
            new
            {
                c.NumeroTicket,
                c.ClientId,
                c.UtilisateurId,
                DateDepot = Naif(c.DateDepot),
                DatePrevue = Naif(c.DatePrevue),
                Statut = c.Statut.ToString(),   // les enums sont enregistrés en texte
                c.Urgent,
                c.RemisePourcent,
                c.MontantTotal,
                c.MontantPaye,
                c.Remarque
            }, tx);

        foreach (var l in c.Lignes)
        {
            l.CommandeId = id;
            // le prix unitaire est COPIÉ dans la ligne (règle 8.2)
            cx.Execute(
                @"INSERT INTO ligne_commande
                    (commande_id, type_vetement_id, service_id, quantite, prix_unitaire,
                     couleur, remarque, photo)
                  VALUES
                    (@CommandeId, @TypeVetementId, @ServiceId, @Quantite, @PrixUnitaire,
                     @Couleur, @Remarque, @Photo)",
                new
                {
                    l.CommandeId,
                    l.TypeVetementId,
                    l.ServiceId,
                    l.Quantite,
                    l.PrixUnitaire,
                    l.Couleur,
                    l.Remarque,
                    l.Photo
                }, tx);
        }

        // première ligne de l'historique : pas d'ancien statut
        cx.Execute(
            @"INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, utilisateur_id)
              VALUES (@id, NULL, @nouveau, @utilisateurId)",
            new { id, nouveau = c.Statut.ToString(), utilisateurId = c.UtilisateurId }, tx);

        tx.Commit();
        c.Id = id;
        return id;
    }

    // ------------------------------------------------------------------
    // Lecture
    // ------------------------------------------------------------------

    // Une commande complète : ClientNom + Lignes (avec les photos).
    public static Commande? ObtenirParId(int id)
    {
        using var cx = DbConnectionFactory.Creer();
        var c = cx.QueryFirstOrDefault<Commande>(SqlBase + " WHERE c.id = @id", new { id });
        if (c != null) ChargerLignes(cx, c);
        return c;
    }

    // Recherche par numéro de ticket (sans tenir compte des majuscules). null si inconnu.
    public static Commande? ObtenirParTicket(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket)) return null;

        using var cx = DbConnectionFactory.Creer();
        var c = cx.QueryFirstOrDefault<Commande>(
            SqlBase + " WHERE UPPER(c.numero_ticket) = UPPER(@ticket)",
            new { ticket = ticket.Trim() });
        if (c != null) ChargerLignes(cx, c);
        return c;
    }

    // Liste filtrable : texte dans le ticket ou le nom du client, statut optionnel.
    // On ne charge pas les lignes ici (les photos alourdiraient la liste).
    public static List<Commande> Rechercher(string texte, StatutCommande? statut)
    {
        var sql = SqlBase + " WHERE 1 = 1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(texte))
        {
            sql += " AND (c.numero_ticket ILIKE @motif OR cl.nom ILIKE @motif)";
            p.Add("motif", "%" + texte.Trim() + "%");
        }

        if (statut != null)
        {
            sql += " AND c.statut = @statut";
            p.Add("statut", statut.Value.ToString());
        }

        sql += " ORDER BY c.date_depot DESC";

        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Commande>(sql, p).ToList();
    }

    // Historique d'un client : les plus récentes d'abord.
    public static List<Commande> ObtenirParClient(int clientId)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Commande>(
            SqlBase + " WHERE c.client_id = @clientId ORDER BY c.date_depot DESC",
            new { clientId }).ToList();
    }

    // Règle 8.3 : date prévue dépassée et linge pas encore rendu ni annulé.
    public static List<Commande> ObtenirEnRetard()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Commande>(
            SqlBase + @" WHERE c.date_prevue < @maintenant
                           AND c.statut IN ('Recu', 'EnTraitement', 'Pret')
                         ORDER BY c.date_prevue",
            new { maintenant = Naif(DateTime.Now) }).ToList();
    }

    // Journal des changements de statut (pour le panneau de détail).
    public static List<HistoriqueStatut> ObtenirHistorique(int commandeId)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<HistoriqueStatut>(
            @"SELECT h.*, u.nom AS utilisateur_nom
              FROM historique_statut h
              LEFT JOIN utilisateur u ON u.id = h.utilisateur_id
              WHERE h.commande_id = @commandeId
              ORDER BY h.date_changement, h.id",
            new { commandeId }).ToList();
    }

    // ------------------------------------------------------------------
    // Changement de statut
    // ------------------------------------------------------------------

    // Met à jour le statut, écrit l'historique et renseigne date_retrait si Retire.
    // Les règles (transition autorisée, solde) sont vérifiées AVANT, par les services.
    public static void ChangerStatut(int commandeId, StatutCommande nouveau, int utilisateurId)
    {
        using var cx = DbConnectionFactory.Creer();
        using var tx = cx.BeginTransaction();

        // FOR UPDATE verrouille la ligne pendant la transaction
        string? ancien = cx.QueryFirstOrDefault<string>(
            "SELECT statut FROM commande WHERE id = @commandeId FOR UPDATE",
            new { commandeId }, tx);

        if (ancien == null)
            throw new InvalidOperationException("Commande introuvable.");

        if (nouveau == StatutCommande.Retire)
        {
            // l'heure de retrait est enregistrée au moment où le linge est rendu
            cx.Execute(
                "UPDATE commande SET statut = @statut, date_retrait = @maintenant WHERE id = @commandeId",
                new { statut = nouveau.ToString(), maintenant = Naif(DateTime.Now), commandeId }, tx);
        }
        else
        {
            cx.Execute(
                "UPDATE commande SET statut = @statut WHERE id = @commandeId",
                new { statut = nouveau.ToString(), commandeId }, tx);
        }

        cx.Execute(
            @"INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, utilisateur_id)
              VALUES (@commandeId, @ancien, @nouveau, @utilisateurId)",
            new { commandeId, ancien, nouveau = nouveau.ToString(), utilisateurId }, tx);

        tx.Commit();
    }

    // ------------------------------------------------------------------
    // Utilitaires
    // ------------------------------------------------------------------

    // Nombre de commandes créées ce jour-là (sert au numéro de ticket, règle 8.1).
    public static int CompterDuJour(DateTime jour)
    {
        using var cx = DbConnectionFactory.Creer();
        const string sql = "SELECT COUNT(*) FROM commande WHERE date_depot >= @debut AND date_depot < @fin";
        return cx.ExecuteScalar<int>(sql, new { debut = Naif(jour.Date), fin = Naif(jour.Date.AddDays(1)) });
    }

    // Remplit commande.Lignes avec les noms du vêtement et du service.
    private static void ChargerLignes(NpgsqlConnection cx, Commande c)
    {
        c.Lignes = cx.Query<LigneCommande>(
            @"SELECT l.*, t.nom AS type_vetement_nom, s.nom AS service_nom
              FROM ligne_commande l
              JOIN type_vetement t ON t.id = l.type_vetement_id
              JOIN service s ON s.id = l.service_id
              WHERE l.commande_id = @id
              ORDER BY l.id",
            new { id = c.Id }).ToList();
    }
}
