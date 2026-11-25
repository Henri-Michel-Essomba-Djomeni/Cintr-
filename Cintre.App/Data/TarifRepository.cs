using Cintre.App.Models;

namespace Cintre.App.Data;

// Propriétaire : Raïssa
public static class TarifRepository
{
    public static List<Service> ObtenirServices() => throw new NotImplementedException();
    public static int AjouterService(Service s) => throw new NotImplementedException();
    public static void ModifierService(Service s) => throw new NotImplementedException();
    public static List<TypeVetement> ObtenirTypesVetement() => throw new NotImplementedException();
    public static int AjouterTypeVetement(TypeVetement t) => throw new NotImplementedException();
    public static decimal? ObtenirPrix(int serviceId, int typeVetementId) => throw new NotImplementedException();
    public static List<Tarif> ObtenirTarifs() => throw new NotImplementedException();
    public static void DefinirPrix(int serviceId, int typeVetementId, decimal prix) => throw new NotImplementedException();
}