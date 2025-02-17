using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NumberIt.ViewModels
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
                TProperty field = (TProperty)propertyInfo.GetValue(obj);

                if (!EqualityComparer<TProperty>.Default.Equals(field, value))
                {
                    propertyInfo.SetValue(obj, value);
                    OnPropertyChanged(name);
                }
            }
        }
        public void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                             




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