using System.Diagnostics;
using System.Windows.Input;

namespace AutoNumber.ViewModels
{
    public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute) : ICommand
    {
        readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        readonly Predicate<object?>? _canExecute = canExecute;

        public RelayCommand(Action<object?> execute)
            : this(execute, null)
        {
        }

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
