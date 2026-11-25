using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;

namespace Cintre.App.Data;

// Propriétaire : Belvian
public static class UtilisateurRepository
{
    public static Utilisateur? ObtenirParLogin(string login)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.QuerySingleOrDefault<Utilisateur>(
            "SELECT * FROM utilisateur WHERE login = @login",
            new { login });
    }

    public static List<Utilisateur> ObtenirTous() => throw new NotImplementedException();
    public static int Ajouter(Utilisateur u) => throw new NotImplementedException();
    public static void Modifier(Utilisateur u) => throw new NotImplementedException();
    public static void ChangerEtat(int id, bool actif) => throw new NotImplementedException();
    public static void ChangerMotDePasse(int id, string hash) => throw new NotImplementedException();
}