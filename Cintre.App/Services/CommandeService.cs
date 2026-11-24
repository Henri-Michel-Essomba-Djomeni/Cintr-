using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Hugo
public static class CommandeService
{
    public static string GenererNumeroTicket() => throw new NotImplementedException();
    public static decimal CalculerTotal(List<LigneCommande> lignes, bool urgent, decimal remisePourcent) => throw new NotImplementedException();
    public static DateTime CalculerDatePrevue(DateTime depot, bool urgent) => throw new NotImplementedException();
    public static bool TransitionAutorisee(StatutCommande actuel, StatutCommande nouveau, bool estAdmin) => throw new NotImplementedException();
    public static void Annuler(int commandeId, int utilisateurId) => throw new NotImplementedException();
}