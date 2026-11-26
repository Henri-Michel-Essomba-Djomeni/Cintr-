using System.Globalization;
using Cintre.App.Data;

namespace Cintre.App.Services;

// Propriétaire : Belvian. Valeurs par défaut utilisées tant que la base n'est pas lue.
public static class ParametresService
{
    public static string NomPressing { get; private set; } = "Cintré";
    public static string Adresse { get; private set; } = "Yaoundé, Cameroun";
    public static string Telephone { get; private set; } = "+237 600 000 000";
    public static string Devise { get; private set; } = "FCFA";

    public static int DelaiStandardJours { get; private set; } = 3;
    public static decimal SupplementUrgencePourcent { get; private set; } = 30;
    public static int SeuilPointsRemise { get; private set; } = 50;
    public static decimal RemiseFideliteMax { get; private set; } = 10;
    public static int JoursAbandon { get; private set; } = 30;

    // Relit la base. Si une valeur manque ou est illisible, on garde la valeur actuelle.
    public static void Recharger()
    {
        NomPressing = Texte("nom_pressing", NomPressing);
        Adresse = Texte("adresse", Adresse);
        Telephone = Texte("telephone", Telephone);
        Devise = Texte("devise", Devise);

        DelaiStandardJours = Entier("delai_standard_jours", DelaiStandardJours);
        SupplementUrgencePourcent = Nombre("supplement_urgence_pourcent", SupplementUrgencePourcent);
        SeuilPointsRemise = Entier("seuil_points_remise", SeuilPointsRemise);
        RemiseFideliteMax = Nombre("remise_fidelite_pourcent", RemiseFideliteMax);
        JoursAbandon = Entier("jours_abandon", JoursAbandon);
    }

    private static string Texte(string cle, string defaut)
    {
        var valeur = ParametreRepository.Obtenir(cle);
        return string.IsNullOrWhiteSpace(valeur) ? defaut : valeur;
    }

    private static int Entier(string cle, int defaut)
        => int.TryParse(ParametreRepository.Obtenir(cle), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var n) ? n : defaut;

    private static decimal Nombre(string cle, decimal defaut)
        => decimal.TryParse(ParametreRepository.Obtenir(cle), NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var n) ? n : defaut;
}