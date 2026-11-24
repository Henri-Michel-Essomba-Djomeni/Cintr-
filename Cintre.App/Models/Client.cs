namespace Cintre.App.Models;

public class Client
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Telephone { get; set; } = "";
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public int PointsFidelite { get; set; }
    public DateTime CreeLe { get; set; }
}