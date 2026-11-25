using Cintre.App.Models;

namespace Cintre.App.Data;

// Propriétaire : Valdez
public static class PaiementRepository
{
    public static int Enregistrer(Paiement p) => throw new NotImplementedException();
    public static List<Paiement> ObtenirParCommande(int commandeId) => throw new NotImplementedException();
    public static List<Paiement> ObtenirParJour(DateTime jour) => throw new NotImplementedException();
    public static List<Commande> ObtenirCommandesImpayees() => throw new NotImplementedException();
}