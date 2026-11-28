using Cintre.App.Models;

namespace Cintre.App.Services;

// Utilisateur connecté et pressing courant. Remplie par AuthService.Connecter (via Ouvrir).
public static class Session
{
    private static Pressing? _pressingChoisi;       // SuperAdmin : pressing sélectionné (vide = mode Tous)
    private static Pressing? _pressingUtilisateur;  // autres rôles : leur pressing, chargé à la connexion

    // Déclenché à chaque changement de pressing courant
    public static event Action? ContexteChange;

    public static Utilisateur? UtilisateurCourant { get; set; }

    public static bool EstAdmin => UtilisateurCourant?.Role == RoleUtilisateur.Admin;
    public static bool EstSuperAdmin => UtilisateurCourant?.Role == RoleUtilisateur.SuperAdmin;

    // TEMPORAIRE : tant qu'il est faux, l'application sans utilisateur travaille sur le pressing 1
    // (pour continuer à développer). T33 le met à vrai au démarrage.
    public static bool ConnexionObligatoire { get; set; }

    // Un SuperAdmin travaille sur le pressing choisi (ou sur tous) ; les autres, sur le leur
    public static int? PressingCourantId => EstSuperAdmin ? _pressingChoisi?.Id : UtilisateurCourant?.PressingId;
    public static string? PressingCourantNom => EstSuperAdmin ? _pressingChoisi?.Nom : _pressingUtilisateur?.Nom;
    public static string? PressingCourantCode => EstSuperAdmin ? _pressingChoisi?.Code : _pressingUtilisateur?.Code;

    public static string LibelleContexte => PressingCourantNom
        ?? (PressingCourantId is null ? "Tous les pressings" : $"Pressing n°{PressingCourantId}");

    // Faux en mode Tous : on peut consulter, pas créer
    public static bool PeutEcrire => PressingCourantId is not null;

    // Appelée par AuthService.Connecter après un mot de passe valide
    public static void Ouvrir(Utilisateur utilisateur, Pressing? pressing)
    {
        UtilisateurCourant = utilisateur;
        _pressingUtilisateur = pressing;
        _pressingChoisi = null;   // un SuperAdmin démarre en mode Tous
        ContexteChange?.Invoke();
    }

    // SuperAdmin seulement ; null = mode Tous
    public static void ChoisirPressing(Pressing? pressing)
    {
        if (!EstSuperAdmin)
            throw new UnauthorizedAccessException("Seul un SuperAdmin peut changer de pressing.");
        _pressingChoisi = pressing;
        ContexteChange?.Invoke();
    }

    // Appelée par AuthService.Deconnecter
    public static void Fermer()
    {
        UtilisateurCourant = null;
        _pressingUtilisateur = null;
        _pressingChoisi = null;
        ContexteChange?.Invoke();
    }
}