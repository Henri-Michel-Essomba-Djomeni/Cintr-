namespace Cintre.App.Models;

// Les réglages de connexion à la base PostgreSQL (écran « Connexion à la base », annexe B)
public class ConfigBase
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Base { get; set; } = "cintre_db";
    public string Utilisateur { get; set; } = "cintre_user";
    public string MotDePasse { get; set; } = "Cintre#2026";
}
