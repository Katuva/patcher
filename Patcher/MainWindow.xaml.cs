using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Patcher;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenFile(TextBox textBox)
    {
        var openFileDialog = new OpenFileDialog();
        
        if (openFileDialog.ShowDialog() == true)
        {
            textBox.Text = openFileDialog.FileName;
        }
    }

    private void OldFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFile(OldFileTextBox);
    }

    private void NewFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFile(NewFileTextBox);
    }

    private void OutputFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folderDialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        if (folderDialog.ShowDialog() == true)
        {
            OutputFolderTextBox.Text = folderDialog.FolderName;
        }
    }

    private async void GeneratePatchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var diff = new Diff()
        {
            OutputFolder = OutputFolderTextBox.Text
        };
        
        diff.CreatePatchFromFile("patch.bin", OldFileTextBox.Text, NewFileTextBox.Text);
    }
}