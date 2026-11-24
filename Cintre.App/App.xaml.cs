using System.Windows;
using Cintre.App.Services;

namespace Cintre.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // TEMPORAIRE (T04) : on initialise la base avant d'ouvrir la fenêtre.
        // T07 remplacera ce MessageBox par l'écran ConnexionBaseWindow.
        try
        {
            DatabaseInitializer.Initialiser();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Impossible d'initialiser la base :\n" + ex.Message, "Cintré",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }
}
