using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class DepotView : UserControl
{
    public DepotView()
    {
        InitializeComponent();
        DataContext = new DepotViewModel();
    }
}
