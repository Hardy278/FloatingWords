using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using FloatingWords.Visuals;

namespace FloatingWords.Dialogs;

public partial class PromptWindow : Window
{
    public PromptWindow(string title, string label, string initialValue)
    {
        InitializeComponent();
        InitializeNoiseTexture();

        Title = title;
        TitleText.Text = title;
        LabelText.Text = label;
        ValueBox.Text = initialValue;

        Loaded += (_, _) =>
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        };
    }

    public string Value => ValueBox.Text;

    private void InitializeNoiseTexture() => CardChrome.ApplyNoise(NoiseLayer);

    private void CardClip_SizeChanged(object sender, SizeChangedEventArgs e) =>
        CardChrome.ApplyClip(CardClip, e.NewSize);

    private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (IsInteractiveSource(e.OriginalSource as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static bool IsInteractiveSource(DependencyObject? source)
    {
        for (DependencyObject? current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase or ScrollBar or Thumb or TextBoxBase or ComboBox)
            {
                return true;
            }
        }

        return false;
    }

    private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ErrorText is not null && ErrorText.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(ValueBox.Text))
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ValueBox.Text))
        {
            ErrorText.Visibility = Visibility.Visible;
            ValueBox.Focus();
            return;
        }

        DialogResult = true;
    }
}
