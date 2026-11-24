namespace Cintre.App.Services;

// Propriétaire : Valdez
public class ResumeCaisse
{
    public decimal Total { get; set; }
    public decimal TotalEspeces { get; set; }
    public decimal TotalMobileMoney { get; set; }
    public decimal TotalCarte { get; set; }
    public int NbPaiements { get; set; }
}

public static class CaisseService
{
    public static ResumeCaisse ObtenirResumeJournee(DateTime jour) => throw new NotImplementedException();
}