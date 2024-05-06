using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;

namespace Patcher;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string? _newTextBoxText;
    private string? _oldTextBoxText;
    private string? _fileToPatchTextBoxText;
    private string? _patchTextBoxText;

    public MainViewModel()
    {
        BrowseCommand = new RelayCommand(BrowseExecute, CanBrowseExecute);
        ProcessCommand = new RelayCommand(ProcessExecute, CanProcessExecute);
        PatchCommand = new RelayCommand(PatchExecute, CanPatchExecute);
    }

    public string? OldTextBoxText
    {
        get => _oldTextBoxText;
        set
        {
            _oldTextBoxText = value;
            OnPropertyChanged(nameof(OldTextBoxText));
        }
    }

    public string? NewTextBoxText
    {
        get => _newTextBoxText;
        set
        {
            _newTextBoxText = value;
            OnPropertyChanged(nameof(NewTextBoxText));
        }
    }
    
    public string? FileToPatchTextBoxText
    {
        get => _fileToPatchTextBoxText;
        set
        {
            _fileToPatchTextBoxText = value;
            OnPropertyChanged(nameof(FileToPatchTextBoxText));
        }
    }
    
    public string? PatchTextBoxText
    {
        get => _patchTextBoxText;
        set
        {
            _patchTextBoxText = value;
            OnPropertyChanged(nameof(PatchTextBoxText));
        }
    }

    public ICommand BrowseCommand { get; set; }
    public ICommand ProcessCommand { get; set; }
    public ICommand PatchCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void BrowseExecute(object parameter)
    {
        var openFileDialog = new OpenFileDialog();

        if (openFileDialog.ShowDialog() != true) return;

        switch (parameter)
        {
            case "new":
                NewTextBoxText = openFileDialog.FileName;
                break;
            case "old":
                OldTextBoxText = openFileDialog.FileName;
                break;
            case "fileToPatch":
                FileToPatchTextBoxText = openFileDialog.FileName;
                break;
            case "patch":
                PatchTextBoxText = openFileDialog.FileName;
                break;
        }
    }

    private static bool CanBrowseExecute(object parameter)
    {
        return true;
    }

    private void ProcessExecute(object parameter)
    {
        var diff = new FileDiff(OldTextBoxText, NewTextBoxText);
        diff.CreateDiff("diff.bin");
    }

    private static bool CanProcessExecute(object parameter)
    {
        return true;
    }
    
    private void PatchExecute(object parameter)
    {
        var patch = new PatchFile(FileToPatchTextBoxText, PatchTextBoxText);
        Task.Run(() => patch.ApplyPatch()).GetAwaiter().GetResult();
    }

    private static bool CanPatchExecute(object parameter)
    {
        return true;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}