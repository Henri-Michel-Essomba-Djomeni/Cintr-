using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cintre.App.Helpers;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? nom = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));

    // Retourne true si la valeur a changé (et notifie l'interface)
    protected bool SetProperty<T>(ref T champ, T valeur, [CallerMemberName] string? nom = null)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur))
            return false;
        champ = valeur;
        OnPropertyChanged(nom);
        return true;
    }
}