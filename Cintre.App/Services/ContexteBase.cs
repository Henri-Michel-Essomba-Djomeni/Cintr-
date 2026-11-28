using Npgsql;

namespace Cintre.App.Services;

// Dit à PostgreSQL pour quel pressing travaille cette connexion (lu par les politiques RLS)
internal static class ContexteBase
{
    public static void Appliquer(NpgsqlConnection cx)
    {
        int? pressing = Session.PressingCourantId;

        // TEMPORAIRE jusqu'à T33 : sans utilisateur connecté, on travaille sur le pressing 1
        if (pressing is null && !Session.EstSuperAdmin && !Session.ConnexionObligatoire)
            pressing = 1;

        using var cmd = new NpgsqlCommand(
            "SELECT set_config('app.pressing_id', @p, false), set_config('app.super_admin', @s, false)", cx);
        cmd.Parameters.AddWithValue("p", pressing?.ToString() ?? string.Empty);
        cmd.Parameters.AddWithValue("s", Session.EstSuperAdmin ? "on" : "off");
        cmd.ExecuteNonQuery();
    }

    // Mode « tous les pressings » : création d'un pressing, initialisation de la base
    public static void AppliquerSuperAdmin(NpgsqlConnection cx)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT set_config('app.pressing_id', '', false), set_config('app.super_admin', 'on', false)", cx);
        cmd.ExecuteNonQuery();
    }
}