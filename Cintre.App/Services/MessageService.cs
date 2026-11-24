using System.Windows;

namespace Cintre.App.Services;

// Point d'entrée unique pour parler à l'utilisateur. Pour l'instant, boîtes Windows simples.
// T27 remplacera l'intérieur par ToastService et une fenêtre de confirmation au style Cintré :
// les appelants (Info, Succes, Erreur, Confirmer) ne changeront pas.
public static class MessageService
{
    public static void Info(string message) => Afficher(message, MessageBoxImage.Information);

    public static void Succes(string message) => Afficher(message, MessageBoxImage.Information);

    public static void Erreur(string message) => Afficher(message, MessageBoxImage.Error);

    public static bool Confirmer(string question)
        => MessageBox.Show(question, "Cintré", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void Afficher(string message, MessageBoxImage icone)
        => MessageBox.Show(message, "Cintré", MessageBoxButton.OK, icone);
}