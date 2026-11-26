using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace Cintre.App.Helpers;

// Génère l'image d'un QR code, prête à afficher dans un <Image> (ticket, écran Retrait).
public static class QrHelper
{
    public static BitmapSource GenererQr(string contenu, int pixelsParModule = 8)
    {
        using var generateur = new QRCodeGenerator();
        using var donnees = generateur.CreateQrCode(contenu, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(donnees).GetGraphic(pixelsParModule);

        using var flux = new MemoryStream(png);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // lit tout de suite : on peut fermer le flux
        image.StreamSource = flux;
        image.EndInit();
        image.Freeze();
        return image;
    }
}