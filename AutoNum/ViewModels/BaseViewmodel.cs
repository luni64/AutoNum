using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AutoNumber.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(name);
            }
        }

        protected void SetProperty<T, TProperty>(T obj, Expression<Func<T, TProperty>> propertySelector, TProperty value, [CallerMemberName] string name = "")
        {
             var propertyInfo = (propertySelector.Body as MemberExpression)?.Member as System.Reflection.PropertyInfo;
            if (propertyInfo != null)
            {
                TProperty? field = (TProperty?)(propertyInfo.GetValue(obj));

                if (!EqualityComparer<TProperty>.Default.Equals(field, value))
                {
                    propertyInfo.SetValue(obj, value);
                    OnPropertyChanged(name);
                }
            }
        }
        public void OnPropertyChanged([CallerMemberName] string name = "")
        {
            if (_notificationSuspendDepth > 0)
            {
                _hasPendingNotification = true;
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Suspends individual PropertyChanged notifications until the returned scope is disposed,
        /// then raises a single PropertyChanged(null) ("assume everything changed") if anything was
        /// suppressed. Use around a block that sets several properties as one logical operation
        /// (e.g. restoring saved state) so listeners recompute once instead of once per property.
        /// Reentrant: nested scopes only flush when the outermost one disposes.
        /// </summary>
        protected IDisposable SuspendNotifications() => new NotificationSuspension(this);

        private int _notificationSuspendDepth;
        private bool _hasPendingNotification;

        private sealed class NotificationSuspension : IDisposable
        {
            private readonly BaseViewModel owner;
            private bool _disposed;

            public NotificationSuspension(BaseViewModel owner)
            {
                this.owner = owner;
                owner._notificationSuspendDepth++;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                if (--owner._notificationSuspendDepth == 0 && owner._hasPendingNotification)
                {
                    owner._hasPendingNotification = false;
                    owner.OnPropertyChanged(string.Empty);
                }
            }
        }

        #endregion


        public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute) : ICommand
        {
            #region Fields
            readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            readonly Predicate<object?>? _canExecute = canExecute;
            #endregion // Fields

            #region Constructors

            public RelayCommand(Action<object?> execute)
                : this(execute, null)
            {
            }
            #endregion // Constructors

            #region ICommand Members

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

            #endregion // ICommand Members
        }

        public class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
        {
            private readonly Func<Task> _execute = execute;
            private readonly Func<bool> _canExecute = canExecute;
            private bool _isExecuting;

            public AsyncCommand(Func<Task> execute) : this(execute, () => true)
            {
            }

            public bool CanExecute(object? parameter)
            {
                return !(_isExecuting && _canExecute());
            }

            public event EventHandler? CanExecuteChanged;

            public async void Execute(object? parameter)
            {
                _isExecuting = true;
                OnCanExecuteChanged();
                try
                {
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                    OnCanExecuteChanged();
                }
            }

            protected virtual void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());
        }


    }
}