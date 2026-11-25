namespace Cintre.App.Models;

public class Commande
{
    public int Id { get; set; }
    public string NumeroTicket { get; set; } = "";
    public int ClientId { get; set; }
    public int UtilisateurId { get; set; }
    public DateTime DateDepot { get; set; }
    public DateTime DatePrevue { get; set; }
    // Vide tant que le linge n'est pas retiré
    public DateTime? DateRetrait { get; set; }
    public StatutCommande Statut { get; set; } = StatutCommande.Recu;
    public bool Urgent { get; set; }
    public decimal RemisePourcent { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public string? Remarque { get; set; }

    // Remplis par les Repositories
    public string ClientNom { get; set; } = "";
    public List<LigneCommande> Lignes { get; set; } = new();

    // Calculés (règles 8.3 et 8.5)
    public decimal Reste => MontantTotal - MontantPaye;

    // En retard = date prévue dépassée et linge pas encore rendu ni annulé
    public bool EnRetard =>
        DatePrevue < DateTime.Now &&
        (Statut == StatutCommande.Recu ||
         Statut == StatutCommande.EnTraitement ||
         Statut == StatutCommande.Pret);
}