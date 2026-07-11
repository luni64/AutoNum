using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    }
}