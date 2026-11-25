using System.Windows.Controls;
using Cintre.App.Helpers;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class IncidentsView : UserControl
{
    public IncidentsView()
    {
        InitializeComponent();
        DataContext = new IncidentsViewModel();

        // La carte de déclaration glisse doucement à l'ouverture de l'écran
        Loaded += (_, _) => AnimationHelper.SlideIn(CarteDeclaration);
    }
}