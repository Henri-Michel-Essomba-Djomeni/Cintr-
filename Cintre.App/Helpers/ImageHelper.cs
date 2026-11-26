using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cintre.App.Helpers;

// Propriétaire : Hugo
public static class ImageHelper
{
    // Lit une image sur le disque et la réduit pour que son plus grand côté
    // ne dépasse pas largeurMax pixels (une petite image n'est jamais agrandie).
    // Retourne le résultat en octets (JPEG), prêt à être enregistré dans la base.
    public static byte[] Redimensionner(string chemin, int largeurMax)
    {
        // OnLoad : l'image est lue en entier, le fichier n'est plus verrouillé ensuite
        var original = new BitmapImage();
        original.BeginInit();
        original.CacheOption = BitmapCacheOption.OnLoad;
        original.UriSource = new Uri(chemin);
        original.EndInit();
        original.Freeze();

        double plusGrandCote = Math.Max(original.PixelWidth, original.PixelHeight);
        double echelle = Math.Min(1.0, largeurMax / plusGrandCote);

        BitmapSource resultat = original;
        if (echelle < 1.0)
            resultat = new TransformedBitmap(original, new ScaleTransform(echelle, echelle));

        // qualité 80 : bon compromis entre netteté et poids dans la base
        var encodeur = new JpegBitmapEncoder { QualityLevel = 80 };
        encodeur.Frames.Add(BitmapFrame.Create(resultat));

        using var flux = new MemoryStream();
        encodeur.Save(flux);
        return flux.ToArray();
    }
}
