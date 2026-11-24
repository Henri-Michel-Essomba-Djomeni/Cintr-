using Cintre.App.Models;

namespace Cintre.App.Services;

// Utilisateur connecté, rempli par AuthService.Connecter
public static class Session
{
    public static Utilisateur? UtilisateurCourant { get; set; }
    public static bool EstAdmin => UtilisateurCourant?.Role == RoleUtilisateur.Admin;
}