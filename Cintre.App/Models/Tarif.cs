namespace Cintre.App.Models;

// Le prix d'un couple (service, type de vêtement), en FCFA
public class Tarif
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int TypeVetementId { get; set; }
    public decimal Prix { get; set; }
}