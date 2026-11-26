using Cintre.App.Data;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Raïssa
public static class FideliteService
{
    // 1 point par tranche de 1000 FCFA (division entière)
    public static int CalculerPoints(decimal montantTotal) => (int)(montantTotal / 1000);

    // Le client a assez de points pour la remise
    public static bool PeutUtiliserRemise(Client c) => c.PointsFidelite >= ParametresService.SeuilPointsRemise;

    // Pourcentage de remise : celui des paramètres si éligible, sinon 0
    public static decimal ObtenirRemisePourcent(Client c) =>
        PeutUtiliserRemise(c) ? ParametresService.RemiseFideliteMax : 0;

    // Appliquer la remise consomme le seuil de points
    public static void UtiliserPoints(int clientId) =>
        ClientRepository.AjouterPoints(clientId, -ParametresService.SeuilPointsRemise);
}
