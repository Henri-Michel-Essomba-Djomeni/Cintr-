namespace Cintre.App.Models;

public class Pressing
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Nom { get; set; } = "";
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public bool Actif { get; set; } = true;
    public DateTime CreeLe { get; set; }
}