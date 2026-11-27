using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Cintre.App.Models;

namespace Cintre.App.Services
{
    // Propriétaire : Hugo
    // Génère et imprime le ticket remis au client au dépôt (règle 8.1 pour le numéro).
    public static class TicketService
    {
        private static readonly CultureInfo Fr = new CultureInfo("fr-FR");

        // Construit le ticket mis en page : pressing, numéro, client, lignes, total, date prévue.
        public static FlowDocument GenererFlowDocument(Commande c)
        {
            // Largeur d'un ticket de caisse (environ 80 mm)
            var doc = new FlowDocument
            {
                PageWidth = 300,
                ColumnWidth = 300,
                PagePadding = new Thickness(12),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            // En-tête : on lit les paramètres de Belvian, avec une valeur de secours
            // si ParametresService n'est pas encore branché (pour ne jamais planter l'impression).
            string nom = Lire(() => ParametresService.NomPressing, "Cintré");
            string adresse = Lire(() => ParametresService.Adresse, "");
            string tel = Lire(() => ParametresService.Telephone, "");
            string devise = Lire(() => ParametresService.Devise, "FCFA");

            doc.Blocks.Add(Centre(nom, 18, true));
            if (!string.IsNullOrWhiteSpace(adresse)) doc.Blocks.Add(Centre(adresse, 11, false));
            if (!string.IsNullOrWhiteSpace(tel)) doc.Blocks.Add(Centre(tel, 11, false));
            doc.Blocks.Add(Separateur());

            // Numéro de ticket en gros : c'est ce que le client présentera au retrait
            doc.Blocks.Add(Centre(c.NumeroTicket, 16, true));
            doc.Blocks.Add(Ligne("Date : " + c.DateDepot.ToString("dd/MM/yyyy HH:mm", Fr)));
            doc.Blocks.Add(Ligne("Client : " + c.ClientNom));
            if (c.Urgent) doc.Blocks.Add(Ligne("*** URGENT ***", true));
            doc.Blocks.Add(Separateur());

            // Tableau des articles : description à gauche, sous-total à droite
            var table = new Table { CellSpacing = 0 };
            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var groupe = new TableRowGroup();
            table.RowGroups.Add(groupe);

            if (c.Lignes != null)
            {
                foreach (var l in c.Lignes)
                {
                    string texte = $"{l.Quantite} x {l.TypeVetementNom} - {l.ServiceNom}";
                    if (!string.IsNullOrWhiteSpace(l.Couleur)) texte += $" ({l.Couleur})";
                    groupe.Rows.Add(LigneTableau(texte, Montant(l.SousTotal, devise), false));
                }
            }
            doc.Blocks.Add(table);
            doc.Blocks.Add(Separateur());

            // Totaux
            var totaux = new Table { CellSpacing = 0 };
            totaux.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            totaux.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var gt = new TableRowGroup();
            totaux.RowGroups.Add(gt);
            if (c.RemisePourcent > 0)
                gt.Rows.Add(LigneTableau("Remise fidélité", "-" + c.RemisePourcent.ToString("0.##", Fr) + " %", false));
            gt.Rows.Add(LigneTableau("TOTAL", Montant(c.MontantTotal, devise), true));
            if (c.MontantPaye > 0)
            {
                gt.Rows.Add(LigneTableau("Déjà payé", Montant(c.MontantPaye, devise), false));
                gt.Rows.Add(LigneTableau("Reste à payer", Montant(c.MontantTotal - c.MontantPaye, devise), true));
            }
            doc.Blocks.Add(totaux);
            doc.Blocks.Add(Separateur());

            // Pied de ticket
            doc.Blocks.Add(Centre("Retrait prévu le", 11, false));
            doc.Blocks.Add(Centre(c.DatePrevue.ToString("dddd dd/MM/yyyy", Fr), 14, true));
            doc.Blocks.Add(Centre("Conservez ce ticket pour le retrait.", 10, false));
            doc.Blocks.Add(Centre("Merci de votre confiance !", 10, false));

            return doc;
        }

        // Ouvre la fenêtre d'impression Windows et imprime le ticket.
        public static void Imprimer(Commande c)
        {
            var dialogue = new PrintDialog();
            if (dialogue.ShowDialog() != true) return; // l'utilisateur a annulé

            var doc = GenererFlowDocument(c);
            // On adapte la mise en page à la zone imprimable de l'imprimante choisie
            doc.PageWidth = dialogue.PrintableAreaWidth;
            doc.PageHeight = dialogue.PrintableAreaHeight;
            doc.ColumnWidth = dialogue.PrintableAreaWidth;

            IDocumentPaginatorSource source = doc;
            dialogue.PrintDocument(source.DocumentPaginator, "Ticket " + c.NumeroTicket);
        }

        // ---------- Petits outils de mise en page ----------

        private static string Montant(decimal valeur, string devise)
        {
            return valeur.ToString("N0", Fr) + " " + devise;
        }

        // Lit un paramètre sans jamais planter (le service de Belvian peut ne pas être prêt)
        private static string Lire(Func<string> lecture, string parDefaut)
        {
            try
            {
                var v = lecture();
                return string.IsNullOrWhiteSpace(v) ? parDefaut : v;
            }
            catch
            {
                return parDefaut;
            }
        }

        private static Paragraph Centre(string texte, double taille, bool gras)
        {
            return new Paragraph(new Run(texte))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = taille,
                FontWeight = gras ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private static Paragraph Ligne(string texte, bool gras = false)
        {
            return new Paragraph(new Run(texte))
            {
                FontWeight = gras ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 1, 0, 1)
            };
        }

        // Trait de séparation sur toute la largeur du ticket
        private static Paragraph Separateur()
        {
            return new Paragraph
            {
                FontSize = 2,
                Margin = new Thickness(0, 4, 0, 4),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        private static TableRow LigneTableau(string gauche, string droite, bool gras)
        {
            var poids = gras ? FontWeights.Bold : FontWeights.Normal;
            var ligne = new TableRow();
            ligne.Cells.Add(new TableCell(new Paragraph(new Run(gauche)) { FontWeight = poids, Margin = new Thickness(0, 1, 0, 1) }));
            ligne.Cells.Add(new TableCell(new Paragraph(new Run(droite)) { FontWeight = poids, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 1, 0, 1) }));
            return ligne;
        }
    }
}
