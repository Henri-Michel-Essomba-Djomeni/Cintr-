using Cintre.App.Models;
using Cintre.App.Services;
using Dapper;
using Npgsql;

namespace Cintre.App.Data;

// Propriétaire : Raïssa
public static class ClientRepository
{
    // Cherche dans le nom et le téléphone, sans tenir compte des majuscules. Texte vide = tous les clients.
    public static List<Client> Rechercher(string texte)
    {
        using var cx = DbConnectionFactory.Creer();
        texte = (texte ?? "").Trim();
        return cx.Query<Client>(
            @"SELECT * FROM client
              WHERE @t = '' OR nom ILIKE '%' || @t || '%' OR telephone ILIKE '%' || @t || '%'
              ORDER BY nom",
            new { t = texte }).ToList();
    }

    public static Client? ObtenirParId(int id)
    {
        using var cx = DbConnectionFactory.Creer();
        return cx.QueryFirstOrDefault<Client>("SELECT * FROM client WHERE id = @id", new { id });
    }

    // Retourne l'id du nouveau client. Refuse un téléphone déjà utilisé.
    public static int Ajouter(Client c)
    {
        using var cx = DbConnectionFactory.Creer();
        VerifierTelephoneLibre(cx, c.Telephone, 0);
        try
        {
            return cx.ExecuteScalar<int>(
                @"INSERT INTO client (nom, telephone, email, adresse)
                  VALUES (@Nom, @Telephone, @Email, @Adresse)
                  RETURNING id",
                new { c.Nom, Telephone = c.Telephone.Trim(), c.Email, c.Adresse });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            // Deux saisies en même temps sur le même numéro
            throw new InvalidOperationException("Ce numéro appartient déjà à un autre client.");
        }
    }

    public static void Modifier(Client c)
    {
        using var cx = DbConnectionFactory.Creer();
        VerifierTelephoneLibre(cx, c.Telephone, c.Id);
        cx.Execute(
            @"UPDATE client
              SET nom = @Nom, telephone = @Telephone, email = @Email, adresse = @Adresse
              WHERE id = @Id",
            new { c.Nom, Telephone = c.Telephone.Trim(), c.Email, c.Adresse, c.Id });
    }

    // Refuse si le client a des commandes
    public static void Supprimer(int id)
    {
        using var cx = DbConnectionFactory.Creer();
        var nb = cx.ExecuteScalar<int>("SELECT COUNT(*) FROM commande WHERE client_id = @id", new { id });
        if (nb > 0)
            throw new InvalidOperationException(
                $"Ce client a {nb} commande(s) : impossible de le supprimer. Gardez-le dans la liste.");
        cx.Execute("DELETE FROM client WHERE id = @id", new { id });
    }

    // Ajoute des points (ou en retire si négatif). Les points ne descendent jamais sous 0.
    public static void AjouterPoints(int clientId, int points)
    {
        using var cx = DbConnectionFactory.Creer();
        cx.Execute(
            "UPDATE client SET points_fidelite = GREATEST(points_fidelite + @points, 0) WHERE id = @clientId",
            new { clientId, points });
    }

    // Lève une exception si ce téléphone appartient déjà à un AUTRE client
    private static void VerifierTelephoneLibre(NpgsqlConnection cx, string telephone, int idClientCourant)
    {
        var nom = cx.QueryFirstOrDefault<string>(
            "SELECT nom FROM client WHERE telephone = @tel AND id <> @id",
            new { tel = (telephone ?? "").Trim(), id = idClientCourant });
        if (nom != null)
            throw new InvalidOperationException($"Ce numéro appartient déjà à {nom}");
    }
}
