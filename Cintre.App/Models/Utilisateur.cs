namespace Cintre.App.Models;

public class Utilisateur
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Login { get; set; } = "";
    // On ne stocke jamais le mot de passe lui-même, seulement son empreinte BCrypt
    public string MotDePasseHash { get; set; } = "";
    public RoleUtilisateur Role { get; set; }
    public bool Actif { get; set; } = true;
    public DateTime CreeLe { get; set; }
    public int? PressingId { get; set; }   // vide pour un SuperAdmin
}