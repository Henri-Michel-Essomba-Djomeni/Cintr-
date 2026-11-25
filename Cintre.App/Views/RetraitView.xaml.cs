using System.Windows.Controls;
using Cintre.App.Helpers;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class RetraitView : UserControl
{
    public RetraitView()
    {
        InitializeComponent();

        var vm = new RetraitViewModel();
        // Animation de succès quand le linge est remis
        vm.RetraitReussi += () => AnimationHelper.SlideIn(BandeauSucces);
        DataContext = vm;

        // Le curseur est directement dans le champ du ticket
        Loaded += (_, _) => ChampTicket.Focus();
    }
}