using Cintre.App.Data;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Belvian
public static class AuthService
{
    public static Utilisateur? Connecter(string login, string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(motDePasse))
            return null;

        var user = UtilisateurRepository.ObtenirParLogin(login.Trim());

        // Même résultat pour "inconnu", "désactivé" et "mauvais mot de passe"
        if (user == null || !user.Actif)
            return null;
        if (!Verifier(motDePasse, user.MotDePasseHash))
            return null;

        Session.UtilisateurCourant = user;
        return user;
    }

    public static void Deconnecter()
    {
        Session.UtilisateurCourant = null;
    }

    public static bool PeutAcceder(string nomEcran)
    {
        var role = Session.UtilisateurCourant?.Role;
        if (role == null) return false;          // personne de connecté

        return role.Value switch
        {
            RoleUtilisateur.Admin => true,
            RoleUtilisateur.Caissier => nomEcran is "Dashboard" or "Commandes" or "Incidents"
                                                 or "Depot" or "Retrait" or "Caisse" or "Clients",
            RoleUtilisateur.Operateur => nomEcran is "Dashboard" or "Commandes" or "Incidents",
            _ => false
        };
    }

    public static string Hacher(string motDePasse)
        => BCrypt.Net.BCrypt.HashPassword(motDePasse);

    public static bool Verifier(string motDePasse, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(motDePasse, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;   // hash mal formé en base : on refuse au lieu de planter
        }
    }
}