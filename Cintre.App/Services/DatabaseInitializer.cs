using System.IO;
using System.Reflection;
using System.Text;
using Dapper;
using Cintre.App.Models;
using Npgsql;

namespace Cintre.App.Services;

public static class DatabaseInitializer
{
    // À appeler au démarrage : crée les tables et les données de départ si la base est vierge
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
