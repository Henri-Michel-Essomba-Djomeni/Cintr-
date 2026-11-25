using Cintre.App.Services;
using Dapper;

namespace Cintre.App.Data;

// Propriétaire : Belvian
public static class ParametreRepository
{
    // Retourne null si la clé n'existe pas
    public static string? Obtenir(string cle)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.QuerySingleOrDefault<string>(
            "SELECT valeur FROM parametre WHERE cle = @cle",
            new { cle });
    }

    // Insère la clé si elle n'existe pas, sinon met à jour sa valeur
    public static void Definir(string cle, string valeur)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute(@"
            INSERT INTO parametre (cle, valeur)
            VALUES (@cle, @valeur)
            ON CONFLICT (cle) DO UPDATE SET valeur = @valeur",
            new { cle, valeur });
    }
}