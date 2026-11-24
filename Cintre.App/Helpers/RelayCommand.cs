using System.Windows.Input;

namespace Cintre.App.Helpers;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _executer;
    private readonly Predicate<object?>? _peutExecuter;

    public RelayCommand(Action<object?> executer, Predicate<object?>? peutExecuter = null)
    {
        _executer = executer ?? throw new ArgumentNullException(nameof(executer));
        _peutExecuter = peutExecuter;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _peutExecuter?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _executer(parameter);
}