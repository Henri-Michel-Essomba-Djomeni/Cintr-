using System.Windows;
using Cintre.App.Services;
using Cintre.App.Views;

namespace Cintre.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Pendant le démarrage, fermer la fenêtre de connexion ne doit pas arrêter l'application
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (!EtablirConnexionBase())
        {
            Shutdown();
            return;
        }

        // Plus tard (T12), c'est ici qu'on ouvrira LoginWindow avant la fenêtre principale
        var fenetre = new MainWindow();
        MainWindow = fenetre;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        fenetre.Show();
    }

    // Teste la base ; si elle ne répond pas, ouvre l'écran de configuration du serveur.
    private static bool EtablirConnexionBase()
    {
        var test = DbConnectionFactory.Tester(ConfigService.Obtenir());

        if (!test.ok)
        {
            var configuration = new ConnexionBaseWindow();
            if (configuration.ShowDialog() != true)
                return false; // fenêtre fermée sans connexion valide
        }

        try
        {
            DatabaseInitializer.Initialiser();
            return true;
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible d'initialiser la base :\n" + ex.Message);
            return false;
        }
    }
}