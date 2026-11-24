namespace Cintre.App.Models;

// Une ligne du journal des changements de statut d'une commande
public class HistoriqueStatut
{
    public int Id { get; set; }
    public int CommandeId { get; set; }
    // Vide pour la toute première ligne (création de la commande)
    public StatutCommande? AncienStatut { get; set; }
    public StatutCommande NouveauStatut { get; set; }
    public DateTime DateChangement { get; set; }
    public int? UtilisateurId { get; set; }
    // Rempli par jointure, pour afficher "par Belvian"
    public string? UtilisateurNom { get; set; }
}