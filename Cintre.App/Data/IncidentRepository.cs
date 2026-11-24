using Cintre.App.Models;

namespace Cintre.App.Data;

// Propriétaire : Valdez
public static class IncidentRepository
{
    public static int Declarer(Incident i) => throw new NotImplementedException();
    public static List<Incident> ObtenirTous(bool seulementOuverts) => throw new NotImplementedException();
    public static List<Incident> ObtenirParCommande(int commandeId) => throw new NotImplementedException();
    public static void Resoudre(int id, string resolution) => throw new NotImplementedException();
    public static List<Commande> ObtenirCommandesAbandonnees(int jours) => throw new NotImplementedException();
}