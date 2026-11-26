using Cintre.App.Models;
using Dapper;

namespace Cintre.App.Services;

// Propriétaire : Raïssa
public static class StatsService
{
    // Chiffres du jour, pour le tableau de bord. Une seule requête pour rester rapide.
    public static ResumeJour ObtenirResumeDuJour()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.QuerySingle<ResumeJour>(
            @"SELECT
                (SELECT COUNT(*) FROM commande
                  WHERE statut <> 'Annule'
                    AND date_depot >= CURRENT_DATE AND date_depot < CURRENT_DATE + 1)::int AS nb_depots,
                (SELECT COUNT(*) FROM commande
                  WHERE statut = 'Retire'
                    AND date_retrait >= CURRENT_DATE AND date_retrait < CURRENT_DATE + 1)::int AS nb_retraits,
                (SELECT COALESCE(SUM(montant), 0) FROM paiement
                  WHERE date_paiement >= CURRENT_DATE AND date_paiement < CURRENT_DATE + 1)::numeric AS recette_jour,
                (SELECT COUNT(*) FROM commande
                  WHERE date_prevue < NOW()
                    AND statut IN ('Recu', 'EnTraitement', 'Pret'))::int AS nb_en_retard,
                (SELECT COUNT(*) FROM commande WHERE statut = 'Pret')::int AS nb_prets_non_retires,
                (SELECT COALESCE(SUM(montant_total - montant_paye), 0) FROM commande
                  WHERE statut <> 'Annule' AND montant_paye < montant_total)::numeric AS total_impayes");
    }

    // Recette (somme des paiements) de chaque jour entre debut et fin inclus.
    // Les jours sans paiement apparaissent avec 0, pour que le graphique n'ait pas de trous.
    public static List<PointStat> ObtenirRecettesParJour(DateTime debut, DateTime fin)
    {
        if (fin < debut) (debut, fin) = (fin, debut);
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<PointStat>(
            @"SELECT to_char(j.jour, 'DD/MM') AS libelle,
                     COALESCE(SUM(p.montant), 0)::numeric AS valeur
              FROM generate_series(@debut::timestamp, @fin::timestamp, interval '1 day') AS j(jour)
              LEFT JOIN paiement p
                     ON p.date_paiement >= j.jour AND p.date_paiement < j.jour + interval '1 day'
              GROUP BY j.jour
              ORDER BY j.jour",
            new
            {
                debut = DateTime.SpecifyKind(debut.Date, DateTimeKind.Unspecified),
                fin = DateTime.SpecifyKind(fin.Date, DateTimeKind.Unspecified)
            }).ToList();
    }

    // Services les plus demandés (nombre de vêtements), commandes annulées exclues
    public static List<PointStat> ObtenirTopServices(int n)
    {
        if (n <= 0) return new List<PointStat>();
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<PointStat>(
            @"SELECT s.nom AS libelle, SUM(l.quantite)::numeric AS valeur
              FROM ligne_commande l
              JOIN commande c ON c.id = l.commande_id
              JOIN service s ON s.id = l.service_id
              WHERE c.statut <> 'Annule'
              GROUP BY s.id, s.nom
              ORDER BY valeur DESC, s.nom
              LIMIT @n", new { n }).ToList();
    }

    // Clients les plus dépensiers (somme des montants des commandes non annulées)
    public static List<PointStat> ObtenirTopClients(int n)
    {
        if (n <= 0) return new List<PointStat>();
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<PointStat>(
            @"SELECT cl.nom AS libelle, SUM(c.montant_total)::numeric AS valeur
              FROM commande c
              JOIN client cl ON cl.id = c.client_id
              WHERE c.statut <> 'Annule'
              GROUP BY cl.id, cl.nom
              ORDER BY valeur DESC, cl.nom
              LIMIT @n", new { n }).ToList();
    }

    // Nombre de commandes par statut
    public static List<PointStat> ObtenirRepartitionStatuts()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<PointStat>(
            @"SELECT statut AS libelle, COUNT(*)::numeric AS valeur
              FROM commande
              GROUP BY statut
              ORDER BY valeur DESC").ToList();
    }
}