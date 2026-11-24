namespace Cintre.App.Models;

public class ResumeJour
{
    public int NbDepots { get; set; }
    public int NbRetraits { get; set; }
    public decimal RecetteJour { get; set; }
    public int NbEnRetard { get; set; }
    public int NbPretsNonRetires { get; set; }
    public decimal TotalImpayes { get; set; }
}