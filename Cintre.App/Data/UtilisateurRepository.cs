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

    // Tous les utilisateurs, actifs ou non
    public static List<Utilisateur> ObtenirTous()
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.Query<Utilisateur>("SELECT * FROM utilisateur ORDER BY nom").ToList();
    }

    // Retourne l'identifiant créé. Le hash est déjà calculé par l'appelant.
    // Un login déjà pris lève une PostgresException (SqlState 23505), à traiter dans le ViewModel.
    public static int Ajouter(Utilisateur u)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.ExecuteScalar<int>(@"
            INSERT INTO utilisateur (nom, login, mot_de_passe_hash, role, actif)
            VALUES (@Nom, @Login, @MotDePasseHash, @Role, @Actif)
            RETURNING id",
            new { u.Nom, u.Login, u.MotDePasseHash, Role = u.Role.ToString(), u.Actif });
    }

    // Seuls le nom et le rôle changent
    public static void Modifier(Utilisateur u)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute(
            "UPDATE utilisateur SET nom = @Nom, role = @Role WHERE id = @Id",
            new { u.Nom, Role = u.Role.ToString(), u.Id });
    }

    public static void ChangerEtat(int id, bool actif)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute("UPDATE utilisateur SET actif = @actif WHERE id = @id", new { id, actif });
    }

    public static void ChangerMotDePasse(int id, string hash)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute("UPDATE utilisateur SET mot_de_passe_hash = @hash WHERE id = @id", new { id, hash });
    }
}