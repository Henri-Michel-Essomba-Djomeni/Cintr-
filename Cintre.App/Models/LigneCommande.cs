namespace Cintre.App.Models;

// Une ligne d'une commande : "3 chemises en lavage"
public class LigneCommande
{
    public int Id { get; set; }
    public int CommandeId { get; set; }
    public int TypeVetementId { get; set; }
    public int ServiceId { get; set; }
    public int Quantite { get; set; }
    // Prix copié au moment du dépôt : un changement de tarif ne touche pas les anciennes commandes (règle 8.2)
    public decimal PrixUnitaire { get; set; }
    public string? Couleur { get; set; }
    public string? Remarque { get; set; }
    // Photo réduite à 800 px max (voir ImageHelper) ; stockée en octets dans la base
    public byte[]? Photo { get; set; }

    // Remplis par jointure SQL, pour l'affichage
    public string TypeVetementNom { get; set; } = "";
    public string ServiceNom { get; set; } = "";

    // Calculé, jamais stocké
    public decimal SousTotal => Quantite * PrixUnitaire;
}