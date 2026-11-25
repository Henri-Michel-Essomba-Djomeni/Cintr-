using System.Windows;

namespace Cintre.App.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow(string message, string libelleOui, bool danger)
    {
        InitializeComponent();
        TxtMessage.Text = message;
        BtnOui.Content = libelleOui;
        BtnOui.Style = (Style)FindResource(danger ? "BoutonDanger" : "BoutonPrimaire");
    }

    private void Oui_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}