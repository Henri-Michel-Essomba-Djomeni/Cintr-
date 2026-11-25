using Dapper;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.Data;

// Propriétaire : Hugo
public static class CommandeRepository
{
    public static int Creer(Commande c) => throw new NotImplementedException();
    public static Commande? ObtenirParId(int id) => throw new NotImplementedException();
    public static Commande? ObtenirParTicket(string ticket) => throw new NotImplementedException();
    public static List<Commande> Rechercher(string texte, StatutCommande? statut) => throw new NotImplementedException();
    public static List<Commande> ObtenirParClient(int clientId) => throw new NotImplementedException();
    public static void ChangerStatut(int commandeId, StatutCommande nouveau, int utilisateurId) => throw new NotImplementedException();
    public static List<Commande> ObtenirEnRetard() => throw new NotImplementedException();

    public static int CompterDuJour(DateTime jour)
{
    using var cx = DbConnectionFactory.Creer();
    const string sql = "SELECT COUNT(*) FROM commande WHERE date_depot >= @debut AND date_depot < @fin";
    return cx.ExecuteScalar<int>(sql, new { debut = jour.Date, fin = jour.Date.AddDays(1) });
}
}