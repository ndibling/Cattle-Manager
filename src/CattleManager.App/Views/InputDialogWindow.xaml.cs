using System.Windows;
using System.Windows.Input;

namespace CattleManager.App.Views;

public partial class InputDialogWindow : Window
{
    public string InputValue => InputBox.Text;

    public InputDialogWindow(string title, string message, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        InputBox.Text = defaultValue;
        Loaded += (_, _) =>
        {
            InputBox.Focus();
            InputBox.SelectAll();
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { DialogResult = true; Close(); }
        if (e.Key == Key.Escape) { DialogResult = false; Close(); }
    }
}
