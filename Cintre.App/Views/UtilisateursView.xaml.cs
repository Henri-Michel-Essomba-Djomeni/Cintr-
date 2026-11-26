using System.Windows.Controls;
using Cintre.App.ViewModels;

namespace Cintre.App.Views;

public partial class UtilisateursView : UserControl
{
    private readonly UtilisateursViewModel _vm = new();

    public UtilisateursView()
    {
        InitializeComponent();
        DataContext = _vm;

        // Un PasswordBox ne se lie pas directement : on recopie la valeur à la main
        MdpForm.PasswordChanged += (_, _) => _vm.MotDePasseForm = MdpForm.Password;
        MdpAncien.PasswordChanged += (_, _) => _vm.AncienMotDePasse = MdpAncien.Password;
        MdpNouveau.PasswordChanged += (_, _) => _vm.NouveauMotDePasse = MdpNouveau.Password;
        MdpConfirmation.PasswordChanged += (_, _) => _vm.ConfirmationMotDePasse = MdpConfirmation.Password;

        _vm.EffacerMotsDePasse += () =>
        {
            MdpForm.Clear();
            MdpAncien.Clear();
            MdpNouveau.Clear();
            MdpConfirmation.Clear();
        };
    }
}