using System.Windows;
using System.Windows.Controls;
using Cintre.App.Helpers;
using Cintre.App.Services;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _vm = new();

    public DashboardView()
    {
        InitializeComponent();
        DataContext = _vm;

        // Un Opérateur ne voit pas la carte Recette : on la retire et on passe à 3 colonnes
        if (!_vm.AfficherRecette)
        {
            Cartes.Children.Remove(CarteRecette);
            Cartes.Columns = 3;
        }

        // La carte « retard » pulse tant qu'il y a des commandes en retard
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.ARetard))
                MettreAJourPulse();
        };

        Loaded += async (_, _) => await _vm.ChargerAsync();
    }

    private void MettreAJourPulse()
    {
        if (_vm.ARetard)
            AnimationHelper.Pulse(CarteRetard);
        else
            AnimationHelper.ArreterPulse(CarteRetard);
    }

    private void NouveauDepot_Click(object sender, RoutedEventArgs e) => NavigationService.Naviguer("Depot");
}