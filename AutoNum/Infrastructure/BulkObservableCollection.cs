using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AutoNumber.Infrastructure
{
    /// <summary>
    /// ObservableCollection that can append many items with a single CollectionChanged (Reset)
    /// notification instead of one per item, so bound handlers don't redo O(n) work per Add.
    /// </summary>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> items)
        {
            var list = items as IReadOnlyCollection<T> ?? items.ToList();
            if (list.Count == 0)
            {
                return;
            }

            foreach (var item in list)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
