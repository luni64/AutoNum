using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoNumber.ViewModels;

namespace AutoNumber.Infrastructure;

/// <summary>
/// Attach to the name-list DataGrid so hovering a row highlights the corresponding marker on the
/// image (via Person.IsSelected), without the "stuck selection" problem a DataGrid SelectedItem
/// has - moving the mouse away naturally clears the highlight.
/// Tracked at the DataGrid level via hit-testing on every mouse move rather than per-row
/// MouseEnter/Leave, because fast mouse movement can skip a recycled row's Leave/Enter pair and
/// leave a stale highlight behind.
/// </summary>
public static class PersonHoverHighlight
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(PersonHoverHighlight),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static readonly DependencyProperty HoveredPersonProperty =
        DependencyProperty.RegisterAttached("HoveredPerson", typeof(Person), typeof(PersonHoverHighlight));

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        grid.PreviewMouseMove -= Grid_PreviewMouseMove;
        grid.MouseLeave -= Grid_MouseLeave;

        if ((bool)e.NewValue)
        {
            grid.PreviewMouseMove += Grid_PreviewMouseMove;
            grid.MouseLeave += Grid_MouseLeave;
        }
    }

    private static void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var hit = grid.InputHitTest(e.GetPosition(grid)) as DependencyObject;
        var row = FindAncestor<DataGridRow>(hit);
        SetHover(grid, row?.DataContext as Person);
    }

    private static void Grid_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            SetHover(grid, null);
        }
    }

    private static void SetHover(DataGrid grid, Person? person)
    {
        var previous = (Person?)grid.GetValue(HoveredPersonProperty);
        if (previous == person)
        {
            return;
        }

        if (previous is not null)
        {
            previous.IsSelected = false;
        }

        if (person is not null)
        {
            person.IsSelected = true;
        }

        grid.SetValue(HoveredPersonProperty, person);
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match)
            {
                return match;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }
}
