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

    public static void Recharger()
    {
        // À faire (Belvian) : relire les valeurs avec ParametreRepository
    }
}