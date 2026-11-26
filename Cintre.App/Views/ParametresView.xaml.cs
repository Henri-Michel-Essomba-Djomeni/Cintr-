using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class ParametresView : UserControl
{
    public ParametresView()
    {
        InitializeComponent();
        DataContext = new ParametresViewModel();
    }
}