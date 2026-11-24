using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Belvian
public static class AuthService
{
    public static Utilisateur? Connecter(string login, string motDePasse) => throw new NotImplementedException();
    public static void Deconnecter() => throw new NotImplementedException();
    public static bool PeutAcceder(string nomEcran) => throw new NotImplementedException();
    public static string Hacher(string motDePasse) => throw new NotImplementedException();
    public static bool Verifier(string motDePasse, string hash) => throw new NotImplementedException();
}