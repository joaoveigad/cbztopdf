using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PagePdf.Application.DTOs;
using PagePdf.Application.UseCases;
using PagePdf.Infrastructure.DependencyInjection;

namespace PagePdf.UI;

public partial class MainWindow : Window
{
    private readonly ConvertComicUseCase _useCase;
    private readonly List<QueueItem> _queue = [];
    private bool _isConverting;

    internal Func<string, string, Task> ShowError { get; set; } = default!;

    public MainWindow()
        : this(CreateDefaultUseCase())
    {
    }

    public MainWindow(ConvertComicUseCase useCase)
    {
        _useCase = useCase;
        ShowError = ShowErrorAsync;
        InitializeComponent();
    }

    private static ConvertComicUseCase CreateDefaultUseCase()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddSingleton<ConvertComicUseCase>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ConvertComicUseCase>();
    }

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
        => await OpenArchivesAsync();

    private async void ConvertButton_Click(object? sender, RoutedEventArgs e)
        => await ExportAsync();

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return;
        }

        var paths = new List<string>();
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is not null)
            {
                paths.Add(path);
            }
        }

        if (paths.Count > 0)
        {
            await SelectArchivesAsync(paths);
        }
    }

    private async Task OpenArchivesAsync()
    {
        if (_isConverting)
        {
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select .cbz comic archives",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Comic archives") { Patterns = ["*.cbz"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        var paths = new List<string>();
        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i].TryGetLocalPath();
            if (path is not null)
            {
                paths.Add(path);
            }
        }

        if (paths.Count > 0)
        {
            await SelectArchivesAsync(paths);
        }
    }

    internal async Task SelectArchiveAsync(string archivePath)
        => await SelectArchivesAsync([archivePath]);

    internal async Task SelectArchivesAsync(IEnumerable<string> archivePaths)
    {
        if (_isConverting)
        {
            return;
        }

        var added = false;
        foreach (var archivePath in archivePaths)
        {
            if (!File.Exists(archivePath))
            {
                await ShowError("File not found", $"The selected file was not found:\n{archivePath}");
                continue;
            }

            var alreadyQueued = false;
            for (var i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].ArchivePath == archivePath)
                {
                    alreadyQueued = true;
                    break;
                }
            }

            if (alreadyQueued)
            {
                continue;
            }

            _queue.Add(new QueueItem(archivePath));
            added = true;
        }

        if (!added)
        {
            return;
        }

        RefreshQueueUi();
        UpdateQueueStatus();
    }

    internal void RemoveArchiveFromQueue(string archivePath)
    {
        if (_isConverting)
        {
            return;
        }

        _queue.RemoveAll(item => item.ArchivePath == archivePath);
        RefreshQueueUi();
        UpdateQueueStatus();
    }

    internal void ClearQueue()
    {
        if (_isConverting)
        {
            return;
        }

        _queue.Clear();
        RefreshQueueUi();
        UpdateQueueStatus();
    }

    private void RemoveQueueItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: QueueItem item })
        {
            RemoveArchiveFromQueue(item.ArchivePath);
        }
    }

    private void ClearQueueButton_Click(object? sender, RoutedEventArgs e)
        => ClearQueue();

    private void RefreshQueueUi()
    {
        QueueList.ItemsSource = new List<QueueItem>(_queue);
        QueuePanel.IsVisible = _queue.Count > 0;
        SetBusy(_isConverting);
    }

    private void UpdateQueueStatus()
    {
        StatusText.Text = _queue.Count == 0
            ? "Ready"
            : PendingCount() == 1 ? "1 file queued" : $"{PendingCount()} files queued";
    }

    private int PendingCount()
    {
        var count = 0;
        for (var i = 0; i < _queue.Count; i++)
        {
            if (_queue[i].Status != "done")
            {
                count++;
            }
        }

        return count;
    }

    private List<QueueItem> GetPendingItems()
    {
        var pending = new List<QueueItem>();
        for (var i = 0; i < _queue.Count; i++)
        {
            if (_queue[i].Status != "done")
            {
                pending.Add(_queue[i]);
            }
        }

        return pending;
    }

    private bool HasPendingWork()
        => PendingCount() > 0;

    private async Task ExportAsync()
    {
        if (_isConverting)
        {
            return;
        }

        if (!HasPendingWork())
        {
            await OpenArchivesAsync();
            if (!HasPendingWork())
            {
                return;
            }
        }

        var pending = GetPendingItems();

        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        string? folder = null;
        string? singleOutput = null;

        if (pending.Count == 1)
        {
            var defaultName = Path.GetFileNameWithoutExtension(pending[0].ArchivePath) + ".pdf";
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export PDF as",
                SuggestedFileName = defaultName,
                DefaultExtension = "pdf",
                FileTypeChoices =
                [
                    new FilePickerFileType("PDF documents") { Patterns = ["*.pdf"] },
                ],
            });

            singleOutput = file?.TryGetLocalPath();
            if (singleOutput is null)
            {
                return;
            }
        }
        else
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select output folder",
                AllowMultiple = false,
            });

            folder = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (folder is null)
            {
                return;
            }
        }

        await ConvertAllAsync(folder, singleOutput);
    }

    internal async Task ConvertAsync(string outputPath)
    {
        if (_isConverting || !HasPendingWork())
        {
            return;
        }

        await ConvertAllAsync(folder: null, singleOutput: outputPath);
    }

    private async Task ConvertAllAsync(string? folder, string? singleOutput)
    {
        _isConverting = true;
        SetBusy(true);
        ProgressBar.Value = 0;

        var pending = GetPendingItems();
        var total = pending.Count;
        var processed = 0;

        try
        {
            if (total == 0)
            {
                return;
            }

            for (var i = 0; i < total; i++)
            {
                var item = pending[i];
                var fileName = item.FileName;
                var outputPath = singleOutput
                    ?? Path.Combine(folder!, Path.GetFileNameWithoutExtension(item.ArchivePath) + ".pdf");

                item.Status = "converting";
                RefreshQueueUi();
                StatusText.Text = $"Converting {fileName} ({i + 1}/{total})...";
                ProgressBar.Value = 0;

                var progress = new Progress<int>(value =>
                    StatusText.Text = $"Converting {fileName} ({i + 1}/{total})... {value}%");

                try
                {
                    await Task.Run(() => _useCase.ExecuteAsync(
                        new ConvertComicRequest(item.ArchivePath, outputPath),
                        progress));

                    item.Status = "done";
                    processed++;
                }
                catch (Exception ex)
                {
                    item.Status = "failed";
                    await ShowError("Conversion failed", $"{fileName}:\n{ex.Message}");
                    StatusText.Text = $"Conversion failed at {fileName}";
                    ProgressBar.Value = 0;
                    break;
                }

                RefreshQueueUi();
            }

            if (processed == total)
            {
                StatusText.Text = $"Done — {processed} file(s) converted";
                ProgressBar.Value = 100;
            }
        }
        finally
        {
            _isConverting = false;
            SetBusy(false);
            RefreshQueueUi();
        }
    }

    private void SetBusy(bool busy)
    {
        OpenButton.IsEnabled = !busy;
        ClearQueueButton.IsEnabled = !busy && _queue.Count > 0;
        ConvertButton.IsEnabled = !busy && HasPendingWork();
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var ok = new Button { Content = "OK", MinWidth = 90, Classes = { "primary" } };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400,
        };

        var panel = new StackPanel { Spacing = 16, Margin = new Avalonia.Thickness(24) };
        panel.Children.Add(text);
        ok.Click += (_, _) => Close();
        panel.Children.Add(ok);

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = panel,
            CanResize = false,
        };

        await dialog.ShowDialog(this);
    }

    internal sealed class QueueItem(string archivePath) : INotifyPropertyChanged
    {
        private string _status = "queued";

        public string ArchivePath { get; } = archivePath;

        public string FileName => Path.GetFileName(ArchivePath);

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsQueued));
                OnPropertyChanged(nameof(IsDone));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(IsConverting));
            }
        }

        public bool IsQueued => Status == "queued";

        public bool IsDone => Status == "done";

        public bool IsFailed => Status == "failed";

        public bool IsConverting => Status == "converting";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
