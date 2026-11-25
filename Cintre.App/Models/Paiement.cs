namespace Cintre.App.Models;

public class Paiement
{
    public int Id { get; set; }
    public int CommandeId { get; set; }
    public decimal Montant { get; set; }
    public ModePaiement Mode { get; set; }
    public DateTime DatePaiement { get; set; }
    public int UtilisateurId { get; set; }
}