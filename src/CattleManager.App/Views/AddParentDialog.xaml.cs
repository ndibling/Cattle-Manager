using CattleManager.Core.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace CattleManager.App.Views;

public partial class AddParentDialog : Window
{
    // Results read by caller after ShowDialog() == true
    public int?    SelectedAnimalId { get; private set; }
    public string? ExternalName     { get; private set; }

    public AddParentDialog(string childBarnName, string role, IReadOnlyList<AnimalDto> allAnimals)
    {
        InitializeComponent();
        Title = $"Assign {role} — {childBarnName}";
        TitleText.Text = $"Assign {role} for \"{childBarnName}\"";

        AnimalCombo.ItemsSource = allAnimals;
        if (allAnimals.Count > 0)
            AnimalCombo.SelectedIndex = 0;
    }

    private void InHerdRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (AnimalCombo is null) return;
        AnimalCombo.IsEnabled    = true;
        ExternalNameBox.IsEnabled = false;
        ExternalNameBox.Clear();
    }

    private void ExternalRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (AnimalCombo is null) return;
        AnimalCombo.IsEnabled    = false;
        ExternalNameBox.IsEnabled = true;
        ExternalNameBox.Focus();
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (InHerdRadio.IsChecked == true)
        {
            if (AnimalCombo.SelectedItem is not AnimalDto animal)
            {
                MessageBox.Show("Please select an animal from the list.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedAnimalId = animal.AnimalId;
            ExternalName     = null;
        }
        else
        {
            var name = ExternalNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter the animal's name.", "Name Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedAnimalId = null;
            ExternalName     = name;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ExternalNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Assign_Click(sender, e);
    }
}
