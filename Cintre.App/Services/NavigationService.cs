using System.Windows.Controls;
using Cintre.App.Helpers;

namespace Cintre.App.Services;

// Affiche un écran dans la zone de contenu de la fenêtre principale.
// Convention : l'écran « Clients » est la classe Cintre.App.Views.ClientsView.
public static class NavigationService
{
    public static readonly string[] Ecrans =
    {
        "Dashboard", "Depot", "Commandes", "Retrait", "Caisse", "Incidents",
        "Clients", "Tarifs", "Statistiques", "Utilisateurs", "Parametres"
    };

    private static ContentControl? _zone;

    public static string? EcranCourant { get; private set; }

    // Déclenché après chaque navigation réussie (sert à surligner le menu)
    public static event Action<string>? EcranChange;

    public static void Initialiser(ContentControl zone) => _zone = zone;

    public static void Naviguer(string ecran)
    {
        if (_zone == null)
            throw new InvalidOperationException("NavigationService.Initialiser n'a pas été appelé.");

        var type = Type.GetType($"Cintre.App.Views.{ecran}View");
        if (type == null)
        {
            MessageService.Erreur($"L'écran « {ecran} » n'existe pas.");
            return;
        }

        try
        {
            var vue = (UserControl)Activator.CreateInstance(type)!;
            _zone.Content = vue;
            AnimationHelper.SlideIn(vue, 300);
            EcranCourant = ecran;
            EcranChange?.Invoke(ecran);
        }
        catch (Exception ex)
        {
            var cause = ex.InnerException ?? ex;
            MessageService.Erreur($"Impossible d'ouvrir l'écran « {ecran} » :\n{cause.Message}");
        }
    }
}