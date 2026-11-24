using Cintre.App.Models;

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
}