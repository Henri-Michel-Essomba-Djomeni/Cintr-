using System.Net.Sockets;
using Dapper;
using Cintre.App.Models;
using Npgsql;

namespace Cintre.App.Services;

public static class DbConnectionFactory
{
    static DbConnectionFactory()
    {
        // Relie automatiquement date_prevue (SQL) à DatePrevue (C#), pour toute l'application
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private static string ConstruireChaine(ConfigBase c)
    {
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = c.Host,
            Port = c.Port,
            Database = c.Base,
            Username = c.Utilisateur,
            Password = c.MotDePasse,
            // 5 secondes max pour joindre le serveur : une mauvaise adresse ne doit pas bloquer l'écran
            Timeout = 5,
            CommandTimeout = 30
        };
        return b.ConnectionString;
    }

    // Retourne une connexion déjà ouverte, à utiliser avec « using var cx = ... »
    // Retourne une connexion déjà ouverte, à utiliser avec « using var cx = ... »
    public static NpgsqlConnection Creer()
    {
        var cx = new NpgsqlConnection(ConstruireChaine(ConfigService.Obtenir()));
        cx.Open();

        // TEMPORAIRE (T40) : tant que T41 n'est pas fusionnée, l'application travaille sur le pressing 1.
        // T41 remplace cette ligne par ContexteBase.Appliquer(cx).
        cx.Execute("SELECT set_config('app.pressing_id', '1', false), set_config('app.super_admin', 'off', false)");
        return cx;
    }

    // Teste une configuration sans l'enregistrer (bouton « Tester la connexion »)
    public static (bool ok, string message) Tester(ConfigBase c)
    {
        try
        {
            using var cx = new NpgsqlConnection(ConstruireChaine(c));
            cx.Open();
            cx.ExecuteScalar<int>("SELECT 1");
            return (true, "Connexion réussie.");
        }
        catch (PostgresException ex) when (ex.SqlState == "28P01" || ex.SqlState == "28000")
        {
            return (false, "Identifiant ou mot de passe incorrect.");
        }
        catch (PostgresException ex) when (ex.SqlState == "3D000")
        {
            return (false, $"La base « {c.Base} » n'existe pas sur ce serveur.");
        }
        catch (Exception ex) when (ex is TimeoutException || ex.InnerException is TimeoutException)
        {
            return (false, "Le serveur ne répond pas. Vérifie l'adresse, Tailscale et le pare-feu.");
        }
        catch (Exception ex) when (ex is SocketException || ex.InnerException is SocketException)
        {
            return (false, "Serveur introuvable. Vérifie l'adresse et que PostgreSQL est démarré.");
        }
        catch (Exception ex)
        {
            return (false, "Connexion impossible : " + ex.Message);
        }
    }
}
