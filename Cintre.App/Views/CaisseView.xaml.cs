using System.Windows.Controls;
using Cintre.App.Helpers;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class CaisseView : UserControl
{
    public CaisseView()
    {
        InitializeComponent();
        DataContext = new CaisseViewModel();

        // Le total glisse doucement à l'ouverture de l'écran
        Loaded += (_, _) => AnimationHelper.SlideIn(CarteTotal);
    }
}