namespace Cintre.App.Models;

// Un service du pressing : Lavage, Repassage, Nettoyage à sec...
public class Service
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
    // On désactive un service au lieu de le supprimer, pour garder l'historique
    public bool Actif { get; set; } = true;
}