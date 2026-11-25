using Cintre.App.Data;
using Cintre.App.Models;

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
    // Résumé des encaissements d'une journée, ventilé par mode de paiement.
    // Le total vient de la somme des paiements : il est toujours égal au détail.
    public static ResumeCaisse ObtenirResumeJournee(DateTime jour)
    {
        var paiements = PaiementRepository.ObtenirParJour(jour);

        var resume = new ResumeCaisse { NbPaiements = paiements.Count };

        foreach (var p in paiements)
        {
            resume.Total += p.Montant;

            switch (p.Mode)
            {
                case ModePaiement.Especes:
                    resume.TotalEspeces += p.Montant;
                    break;
                case ModePaiement.MobileMoney:
                    resume.TotalMobileMoney += p.Montant;
                    break;
                case ModePaiement.Carte:
                    resume.TotalCarte += p.Montant;
                    break;
            }
        }

        return resume;
    }
}