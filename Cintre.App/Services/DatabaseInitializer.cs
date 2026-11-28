using System.IO;
using System.Reflection;
using System.Text;
using Dapper;
using Cintre.App.Models;
using Npgsql;

namespace Cintre.App.Services;

public static class DatabaseInitializer
{
    // À appeler au démarrage : crée les tables et les données de départ si la base est vierge,
    // puis applique les migrations numérotées (Database/migrations)
    public static void Initialiser()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        using var cx = DbConnectionFactory.Creer();

        // Base vierge = la table utilisateur n'existe pas encore
        bool premiereFois = cx.ExecuteScalar<string?>("SELECT to_regclass('public.utilisateur')::text") is null;

        // Une seule transaction : si le seed échoue, on ne laisse pas une base à moitié créée
        using (var tx = cx.BeginTransaction())
        {
            // Le schéma est rejouable sans danger (IF NOT EXISTS), on le lance toujours
            cx.Execute(LireScript("schema.sql"), transaction: tx);

            // Le seed ne se lance qu'à la création, sinon il remettrait des services supprimés
            if (premiereFois)
                cx.Execute(LireScript("seed.sql"), transaction: tx);

            tx.Commit();
        }

        CreerAdminParDefaut(cx);

        // Mode « tous les pressings » pour cette connexion : sans cela, la sécurité par ligne
        // installée par la migration 001 bloquerait les migrations suivantes
        cx.Execute("SELECT set_config('app.super_admin', 'on', false), set_config('app.pressing_id', '', false)");
        try
        {
            AppliquerMigrations(cx);
            AssurerSuperAdmin(cx);
        }
        finally
        {
            cx.Execute("SELECT set_config('app.super_admin', 'off', false), set_config('app.pressing_id', '1', false)");
        }
    }

    // Applique, dans l'ordre, les migrations embarquées pas encore enregistrées dans schema_version
    public static void AppliquerMigrations(NpgsqlConnection cx)
    {
        cx.Execute(@"CREATE TABLE IF NOT EXISTS schema_version (
                         version INT PRIMARY KEY,
                         nom VARCHAR(100) NOT NULL,
                         applique_le TIMESTAMP NOT NULL DEFAULT NOW())");

        var dejaAppliquees = cx.Query<int>("SELECT version FROM schema_version").ToHashSet();

        var migrations = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith("migrations.", StringComparison.OrdinalIgnoreCase)
                        && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        foreach (var ressource in migrations)
        {
            string fichier = ressource["migrations.".Length..]; // ex. 001_multi_pressing.sql
            if (fichier.Length < 4 || !int.TryParse(fichier[..3], out int version))
                throw new InvalidOperationException(
                    $"Nom de migration invalide : {fichier} (attendu : trois chiffres, puis _nom.sql).");

            if (dejaAppliquees.Contains(version)) continue;

            using var tx = cx.BeginTransaction();
            try
            {
                cx.Execute(LireScript(ressource), transaction: tx);
                cx.Execute("INSERT INTO schema_version (version, nom) VALUES (@version, @nom)",
                           new { version, nom = fichier }, tx);
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw new InvalidOperationException(
                    $"La migration {fichier} a échoué et a été annulée : {ex.Message}", ex);
            }
        }
    }

    // Crée le compte superadmin s'il n'existe aucun SuperAdmin
    public static void AssurerSuperAdmin(NpgsqlConnection cx)
    {
        int nb = cx.ExecuteScalar<int>("SELECT COUNT(*) FROM utilisateur WHERE role = 'SuperAdmin'");
        if (nb > 0) return;

        // Compte de départ : superadmin / Super#2026 (à changer avant la présentation)
        cx.Execute(
            @"INSERT INTO utilisateur (nom, login, mot_de_passe_hash, role, pressing_id)
              VALUES (@nom, @login, @hash, 'SuperAdmin', NULL)
              ON CONFLICT (login) DO NOTHING",
            new
            {
                nom = "Super administrateur",
                login = "superadmin",
                hash = BCrypt.Net.BCrypt.HashPassword("Super#2026")
            });
    }

    private static void CreerAdminParDefaut(NpgsqlConnection cx)
    {
        int nb = cx.ExecuteScalar<int>("SELECT COUNT(*) FROM utilisateur");
        if (nb > 0) return;

        // Compte de départ : admin / admin123 (à changer avant la présentation)
        cx.Execute(
            "INSERT INTO utilisateur (nom, login, mot_de_passe_hash, role) VALUES (@nom, @login, @hash, @role)",
            new
            {
                nom = "Administrateur",
                login = "admin",
                hash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                role = RoleUtilisateur.Admin.ToString()
            });
    }

    // Les scripts SQL sont embarqués dans l'exécutable (voir Cintre.App.csproj)
    private static string LireScript(string nom)
    {
        using var flux = Assembly.GetExecutingAssembly().GetManifestResourceStream(nom)
            ?? throw new InvalidOperationException($"Script SQL introuvable dans l'exécutable : {nom}");
        using var lecteur = new StreamReader(flux, Encoding.UTF8);
        return lecteur.ReadToEnd();
    }
}