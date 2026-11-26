using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class StatistiquesView : UserControl
{
    public StatistiquesView()
    {
        InitializeComponent();
        DataContext = new StatistiquesViewModel();
    }
}
