using System.Windows;
using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class TarifsView : UserControl
{
    public TarifsView()
    {
        InitializeComponent();
        DataContext = new TarifsViewModel();
    }

    // Quand la petite fenêtre de saisie s'ouvre, le curseur est directement dans le champ du prix
    private void SaisiePopup_Opened(object sender, EventArgs e)
    {
        PrixBox.Focus();
        PrixBox.SelectAll();
    }
}
