namespace Cintre.App.Models;

public class Incident
{
    public int Id { get; set; }
    public int CommandeId { get; set; }
    public TypeIncident Type { get; set; }
    public string Description { get; set; } = "";
    public DateTime DateIncident { get; set; }
    public bool Resolu { get; set; }
    public string? Resolution { get; set; }

    // Remplis par jointure : le doc demande "avec ticket et nom du client" pour ObtenirTous
    public string NumeroTicket { get; set; } = "";
    public string ClientNom { get; set; } = "";
}