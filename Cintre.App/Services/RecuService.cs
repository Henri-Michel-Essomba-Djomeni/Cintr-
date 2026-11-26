using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Cintre.App.Models;

namespace Cintre.App.Services;

// Propriétaire : Valdez
public static class RecuService
{
    // Libellé lisible d'un mode de paiement (utilisé aussi par l'écran Retrait)
    public static string LibelleMode(ModePaiement mode) => mode switch
    {
        ModePaiement.Especes => "Espèces",
        ModePaiement.MobileMoney => "Mobile Money",
        ModePaiement.Carte => "Carte",
        _ => mode.ToString()
    };

    // Imprime le reçu d'un paiement. "c" doit être la commande RECHARGÉE après le paiement,
    // pour que le reste à payer affiché soit juste.
    public static void Imprimer(Paiement p, Commande c)
    {
        try
        {
            var doc = Construire(p, c);

            var dialogue = new PrintDialog();
            if (dialogue.ShowDialog() != true)
                return; // l'utilisateur a annulé

            // On adapte le document à la taille de la page choisie
            doc.PageWidth = dialogue.PrintableAreaWidth;
            doc.PageHeight = dialogue.PrintableAreaHeight;
            doc.PagePadding = new Thickness(40);
            doc.ColumnWidth = dialogue.PrintableAreaWidth;

            dialogue.PrintDocument(
                ((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"Reçu {c.NumeroTicket}");
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impression impossible : " + ex.Message);
        }
    }

    private static FlowDocument Construire(Paiement p, Commande c)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13
        };

        doc.Blocks.Add(Ligne(ParametresService.NomPressing, 20, FontWeights.Bold, TextAlignment.Center));
        doc.Blocks.Add(Ligne("REÇU DE PAIEMENT", 14, FontWeights.SemiBold, TextAlignment.Center));
        doc.Blocks.Add(Ligne("----------------------------------------", 13, FontWeights.Normal, TextAlignment.Center));

        doc.Blocks.Add(Ligne($"Ticket : {c.NumeroTicket}"));
        if (!string.IsNullOrWhiteSpace(c.ClientNom))
            doc.Blocks.Add(Ligne($"Client : {c.ClientNom}"));
        doc.Blocks.Add(Ligne($"Date : {p.DatePaiement:dd/MM/yyyy HH:mm}"));
        doc.Blocks.Add(Ligne($"Mode : {LibelleMode(p.Mode)}"));

        doc.Blocks.Add(Ligne("----------------------------------------", 13, FontWeights.Normal, TextAlignment.Center));

        doc.Blocks.Add(Ligne($"Montant payé : {p.Montant:N0} FCFA", 15, FontWeights.Bold));
        doc.Blocks.Add(Ligne($"Total de la commande : {c.MontantTotal:N0} FCFA"));
        doc.Blocks.Add(Ligne($"Déjà payé : {c.MontantPaye:N0} FCFA"));
        doc.Blocks.Add(Ligne(
            c.Reste > 0 ? $"Reste à payer : {c.Reste:N0} FCFA" : "Commande soldée",
            15, FontWeights.Bold));

        doc.Blocks.Add(Ligne(""));
        doc.Blocks.Add(Ligne("Merci de votre confiance.", 13, FontWeights.Normal, TextAlignment.Center));

        return doc;
    }

    private static Paragraph Ligne(string texte, double taille = 13,
        FontWeight? poids = null, TextAlignment alignement = TextAlignment.Left)
    {
        return new Paragraph(new Run(texte))
        {
            FontSize = taille,
            FontWeight = poids ?? FontWeights.Normal,
            TextAlignment = alignement,
            Margin = new Thickness(0, 2, 0, 2)
        };
    }
}