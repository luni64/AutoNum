using System.Windows;
using System.Windows.Media;

namespace AutoNumber.Infrastructure
{
    public static class VisualTreeHelpers
    {
        /// <summary>Nearest ancestor (including the element itself) of the given type.</summary>
        public static T? FindAncestor<T>(DependencyObject? element) where T : class
        {
            var current = element;
            while (current is not null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        /// <summary>Nearest ancestor (including the element itself) whose DataContext is of the given type.</summary>
        public static T? FindAncestorDataContext<T>(DependencyObject? element) where T : class
        {
            var current = element;
            while (current is not null)
            {
                if (current is FrameworkElement fe && fe.DataContext is T dataContext)
                {
                    return dataContext;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
