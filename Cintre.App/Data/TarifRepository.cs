using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;
using Npgsql;

namespace Cintre.App.Data;

// Propriétaire : Raïssa
public static class TarifRepository
{
    // Services actifs d'abord, puis par nom
    public static List<Service> ObtenirServices()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Service>("SELECT * FROM service ORDER BY actif DESC, nom").ToList();
    }

    public static int AjouterService(Service s)
    {
        using var cx = DbConnectionFactory.Creer();
        try
        {
            return cx.ExecuteScalar<int>(
                "INSERT INTO service (nom, description, actif) VALUES (@Nom, @Description, @Actif) RETURNING id", s);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException($"Le service « {s.Nom} » existe déjà.");
        }
    }

    // La désactivation se fait avec Actif = false (on ne supprime jamais, pour garder l'historique)
    public static void ModifierService(Service s)
    {
        using var cx = DbConnectionFactory.Creer();
        try
        {
            cx.Execute("UPDATE service SET nom = @Nom, description = @Description, actif = @Actif WHERE id = @Id", s);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException($"Le service « {s.Nom} » existe déjà.");
        }
    }

    public static List<TypeVetement> ObtenirTypesVetement()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<TypeVetement>("SELECT * FROM type_vetement ORDER BY nom").ToList();
    }

    public static int AjouterTypeVetement(TypeVetement t)
    {
        using var cx = DbConnectionFactory.Creer();
        try
        {
            return cx.ExecuteScalar<int>("INSERT INTO type_vetement (nom) VALUES (@Nom) RETURNING id", t);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException($"Le type « {t.Nom} » existe déjà.");
        }
    }

    // null = aucun tarif défini (service non proposé pour ce vêtement)
    public static decimal? ObtenirPrix(int serviceId, int typeVetementId)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.ExecuteScalar<decimal?>(
            "SELECT prix FROM tarif WHERE service_id = @serviceId AND type_vetement_id = @typeVetementId",
            new { serviceId, typeVetementId });
    }

    public static List<Tarif> ObtenirTarifs()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Tarif>("SELECT * FROM tarif").ToList();
    }

    // Insère ou met à jour le prix. Un prix négatif est refusé.
    public static void DefinirPrix(int serviceId, int typeVetementId, decimal prix)
    {
        if (prix < 0)
            throw new ArgumentException("Le prix ne peut pas être négatif.");
        using var cx = DbConnectionFactory.Creer();
        cx.Execute(
            @"INSERT INTO tarif (service_id, type_vetement_id, prix)
              VALUES (@serviceId, @typeVetementId, @prix)
              ON CONFLICT (service_id, type_vetement_id) DO UPDATE SET prix = EXCLUDED.prix",
            new { serviceId, typeVetementId, prix });
    }
}
