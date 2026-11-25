using System.Windows;
using Cintre.App.Views;

namespace Cintre.App.Services;

// Point d'entrée unique pour parler à l'utilisateur. Succès, erreur et info passent par les toasts.
public static class MessageService
{
    public static void Info(string message) => Notifier(message, ToastService.Info);

    public static void Succes(string message) => Notifier(message, ToastService.Succes);

    public static void Erreur(string message) => Notifier(message, ToastService.Erreur);

    // Fenêtre de confirmation au style Cintré. danger = true : bouton rouge (suppression, annulation).
    public static bool Confirmer(string question, string libelleOui = "Confirmer", bool danger = false)
    {
        var fenetre = new ConfirmationWindow(question, libelleOui, danger);

        var proprietaire = Application.Current?.MainWindow;
        if (proprietaire is { IsLoaded: true, IsVisible: true })
            fenetre.Owner = proprietaire;
        else
            fenetre.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        return fenetre.ShowDialog() == true;
    }

    // Avant l'ouverture de la fenêtre principale (démarrage), il n'y a pas encore de zone de toasts :
    // on retombe alors sur une boîte Windows, uniquement pour ces erreurs de démarrage.
    private static void Notifier(string message, Action<string> toast)
    {
        if (ToastService.EstPret)
            toast(message);
        else
            MessageBox.Show(message, "Cintré");
    }
}