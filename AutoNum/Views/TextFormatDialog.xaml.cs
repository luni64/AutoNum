using AutoNumber.Infrastructure;
using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using AutoNumber.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.IconPacks;

namespace AutoNumber.Views;

public partial class TextFormatDialog : MetroWindow
{
    private static readonly ColorConverter DrawingToMediaColorConverter = new();
    private static readonly EnumBooleanConverter PlacementConverter = new();
    private object? _manager;
    private string? _managerTypeName;

    public TextFormatDialog(object manager, string dialogTitle, string fontColorPath, string backgroundColorPath, string fontSizePath)
    {
        InitializeComponent();

        _manager = manager;
        _managerTypeName = manager.GetType().Name;
        DataContext = manager;
        Title = dialogTitle;

        // Show "Use as default" button for all supported formatting managers.
        if (_managerTypeName == nameof(LabelManager) ||
            _managerTypeName == nameof(TitleManager) ||
            _managerTypeName == nameof(ImageInfoManager) ||
            _managerTypeName == nameof(ImageIdManager) ||
            _managerTypeName == nameof(NameManager))
        {
            UseAsDefaultButton.Visibility = Visibility.Visible;
        }

        if (manager is NameManager)
        {
            NameTableOptionsPanel.Visibility = Visibility.Visible;

            BindingOperations.SetBinding(
                NamesBelowRadioButton,
                RadioButton.IsCheckedProperty,
                new Binding(nameof(NameManager.Placement))
                {
                    Mode = BindingMode.TwoWay,
                    Converter = PlacementConverter,
                    ConverterParameter = nameof(NamesPlacement.Below)
                });

            BindingOperations.SetBinding(
                NamesRightRadioButton,
                RadioButton.IsCheckedProperty,
                new Binding(nameof(NameManager.Placement))
                {
                    Mode = BindingMode.TwoWay,
                    Converter = PlacementConverter,
                    ConverterParameter = nameof(NamesPlacement.Right)
                });

            foreach (var element in new FrameworkElement[] { NamesSideWidthLabel, NamesSideWidthSlider, NamesSideWidthText })
            {
                BindingOperations.SetBinding(
                    element,
                    IsEnabledProperty,
                    new Binding(nameof(NameManager.Placement))
                    {
                        Mode = BindingMode.OneWay,
                        Converter = PlacementConverter,
                        ConverterParameter = nameof(NamesPlacement.Right)
                    });
            }

            BindingOperations.SetBinding(
                NamesSideWidthSlider,
                Slider.ValueProperty,
                new Binding(nameof(NameManager.SideWidthScale))
                {
                    Mode = BindingMode.TwoWay
                });

            BindingOperations.SetBinding(
                NamesSideWidthText,
                TextBlock.TextProperty,
                new Binding(nameof(NameManager.SideWidthScale))
                {
                    Mode = BindingMode.OneWay,
                    StringFormat = "{0:P0}"
                });

            BindingOperations.SetBinding(
                NameTableColumnCountComboBox,
                ComboBox.SelectedValueProperty,
                new Binding(nameof(NameManager.NameTableColumnCount))
                {
                    Mode = BindingMode.TwoWay
                });

            BindingOperations.SetBinding(
                ShowRowDividersCheckBox,
                CheckBox.IsCheckedProperty,
                new Binding(nameof(NameManager.ShowRowDividers))
                {
                    Mode = BindingMode.TwoWay
                });

            BindingOperations.SetBinding(
                RowDividerTextTemplateTextBox,
                TextBox.TextProperty,
                new Binding(nameof(NameManager.RowDividerTextTemplate))
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
        }

        if (manager is LabelManager labelManager)
        {
            FontManagerControl.ShowEdgeColor = true;
            LabelAnchorPanel.Visibility = Visibility.Visible;

            BindingOperations.SetBinding(
                FaceAnchorPickerControl,
                FaceAnchorPicker.SelectedAnchorProperty,
                new Binding(nameof(LabelManager.FaceLabelAnchor))
                {
                    Mode = BindingMode.TwoWay
                });

            BindingOperations.SetBinding(
                RedetectFacesButton,
                System.Windows.Controls.Primitives.ButtonBase.CommandProperty,
                new Binding(nameof(LabelManager.RedetectFacesCommand)));

            BindingOperations.SetBinding(
                NumberBottomUpCheckBox,
                CheckBox.IsCheckedProperty,
                new Binding(nameof(LabelManager.NumberBottomUp))
                {
                    Mode = BindingMode.TwoWay
                });

            BindingOperations.SetBinding(
                FontManagerControl,
                FontManager.EdgeColorProperty,
                new Binding(nameof(LabelManager.EdgeColor))
                {
                    Mode = BindingMode.TwoWay,
                    Converter = DrawingToMediaColorConverter
                });
        }

        BindFontManager(fontColorPath, backgroundColorPath, fontSizePath);
    }

    private void BindFontManager(string fontColorPath, string backgroundColorPath, string fontSizePath)
    {
        BindingOperations.SetBinding(
            FontManagerControl,
            FontManager.FontColorProperty,
            new Binding(fontColorPath)
            {
                Mode = BindingMode.TwoWay,
                Converter = DrawingToMediaColorConverter
            });

        BindingOperations.SetBinding(
            FontManagerControl,
            FontManager.BackgroundColorProperty,
            new Binding(backgroundColorPath)
            {
                Mode = BindingMode.TwoWay,
                Converter = DrawingToMediaColorConverter
            });

        // Bind the SelectedScale property to the manager's scale property
        // The slider binding is already handled in FontManager.xaml with the converter
        var scaleBinding = new Binding(fontSizePath) { Mode = BindingMode.TwoWay };

        BindingOperations.SetBinding(
            FontManagerControl,
            FontManager.SelectedScaleProperty,
            scaleBinding);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UseAsDefault_Click(object sender, RoutedEventArgs e)
    {
        if (_manager == null || _managerTypeName == null)
        {
            return;
        }

        // Read the current formatting values from the manager
        var (currentScale, fontColor, backgroundColor, edgeColor) = _manager switch
        {
            LabelManager lm => (lm.LabelScale, lm.FontColor, lm.BackgroundColor, (System.Drawing.Color?)lm.EdgeColor),
            TitleManager tm => (tm.FontScale, tm.FontColor, tm.BackgroundColor, (System.Drawing.Color?)null),
            ImageInfoManager iim => (iim.FontScale, iim.FontColor, iim.BackgroundColor, (System.Drawing.Color?)null),
            ImageIdManager idm => (idm.FontScale, idm.FontColor, idm.BackgroundColor, (System.Drawing.Color?)null),
            NameManager nm => (nm.FontScale, nm.FontColor, nm.BackgroundColor, (System.Drawing.Color?)null),
            _ => (1.0, System.Drawing.Color.Black, System.Drawing.Color.White, (System.Drawing.Color?)null)
        };

        // Find the SettingsManager in the application's main view model
        // This is a bit of a hack, but we need access to SettingsManager
        if (Owner is MainWindow mainWindow && mainWindow.DataContext is MainVM mainVM)
        {
            mainVM.SettingsManager.UpdateDefaultFormatting(_managerTypeName, currentScale, fontColor, backgroundColor, edgeColor);

            if (_manager is LabelManager labelManager)
            {
                mainVM.SettingsManager.FaceLabelAnchor = labelManager.FaceLabelAnchor;
                mainVM.SettingsManager.NumberBottomUp = labelManager.NumberBottomUp;
            }

            if (_manager is NameManager nameManager)
            {
                mainVM.SettingsManager.NamesPlacement = nameManager.Placement;
                mainVM.SettingsManager.NamesSideWidthScale = nameManager.SideWidthScale;
            }

            // Setters no longer persist automatically — this button is one of the explicit
            // "the user wants this on disk" moments (the other is the settings dialog's OK).
            mainVM.SettingsManager.Save();

            MessageBox.Show("Die Einstellung wurde als Standardwert übernommen.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
