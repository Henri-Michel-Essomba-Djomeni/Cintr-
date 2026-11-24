using System.IO;
using System.Text.Json;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Forme du fichier appsettings.json : { "Database": { ... } }
internal sealed class RacineConfig
{
    public ConfigBase Database { get; set; } = new();
}

// Lit et écrit appsettings.json, placé à côté de l'exécutable (jamais commité, voir .gitignore)
public static class ConfigService
{
    private static readonly string _chemin = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static ConfigBase Obtenir()
    {
        // Pas de fichier : on retourne les valeurs par défaut (base locale)
        if (!File.Exists(_chemin))
            return new ConfigBase();

        try
        {
            var racine = JsonSerializer.Deserialize<RacineConfig>(File.ReadAllText(_chemin), _options);
            return racine?.Database ?? new ConfigBase();
        }
        catch (JsonException)
        {
            // Fichier abîmé : mieux vaut repartir sur les valeurs par défaut que planter au démarrage
            return new ConfigBase();
        }
    }

    public static void Enregistrer(ConfigBase c)
    {
        var racine = new RacineConfig { Database = c };
        File.WriteAllText(_chemin, JsonSerializer.Serialize(racine, _options));
    }
}
