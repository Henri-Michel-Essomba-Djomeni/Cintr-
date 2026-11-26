using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class ClientsView : UserControl
{
    public ClientsView()
    {
        InitializeComponent();
        DataContext = new ClientsViewModel();
    }
}
