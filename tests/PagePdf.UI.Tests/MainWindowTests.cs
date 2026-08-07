using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using PagePdf.Application.DTOs;
using PagePdf.Application.Interfaces;
using PagePdf.Application.UseCases;
using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using Xunit;

namespace PagePdf.UI.Tests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_has_open_and_convert_buttons()
    {
        var window = new MainWindow(CreateUseCase(1));

        Assert.NotNull(window.FindControl<Button>("OpenButton"));
        Assert.NotNull(window.FindControl<Button>("ConvertButton"));
    }

    [AvaloniaFact]
    public async Task MainWindow_select_archive_enables_export()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz = CreateTempCbz();

        try
        {
            await window.SelectArchiveAsync(tempCbz);

            Assert.True(window.FindControl<Button>("ConvertButton")!.IsEnabled);
            Assert.Equal("1 file queued",
                window.FindControl<TextBlock>("StatusText")!.Text);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_select_multiple_archives_queues_all()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz1 = CreateTempCbz();
        var tempCbz2 = CreateTempCbz();

        try
        {
            await window.SelectArchivesAsync([tempCbz1, tempCbz2]);

            var queue = window.FindControl<ItemsControl>("QueueList");
            Assert.NotNull(queue);
            Assert.True(queue!.IsVisible);
            Assert.Equal(2, queue.Items?.Count);
            Assert.True(window.FindControl<Button>("ConvertButton")!.IsEnabled);
            Assert.Equal("2 files queued",
                window.FindControl<TextBlock>("StatusText")!.Text);
        }
        finally
        {
            File.Delete(tempCbz1);
            File.Delete(tempCbz2);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_select_same_archive_does_not_duplicate()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz = CreateTempCbz();

        try
        {
            await window.SelectArchivesAsync([tempCbz, tempCbz]);

            var queue = window.FindControl<ItemsControl>("QueueList");
            Assert.Equal(1, queue!.Items?.Count);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_select_missing_archive_shows_error()
    {
        var window = new MainWindow(CreateUseCase(1));
        var errors = new List<string>();
        window.ShowError = (title, message) =>
        {
            errors.Add($"{title}: {message}");
            return Task.CompletedTask;
        };

        await window.SelectArchiveAsync(Path.Combine(Path.GetTempPath(), "missing.cbz"));

        Assert.Contains(errors, e => e.StartsWith("File not found:"));
        Assert.False(window.FindControl<Button>("ConvertButton")!.IsEnabled);
    }

    [AvaloniaFact]
    public async Task MainWindow_convert_updates_progress_and_status()
    {
        var tempCbz = CreateTempCbz();
        var outputPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var window = new MainWindow(CreateUseCase(3));
            await window.SelectArchiveAsync(tempCbz);
            await window.ConvertAsync(outputPdf);

            Assert.Equal(100, window.FindControl<ProgressBar>("ProgressBar")!.Value);
            Assert.Contains("Done", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.True(window.FindControl<Button>("OpenButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz);
            File.Delete(outputPdf);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_convert_failure_shows_error_and_reports_status()
    {
        var tempCbz = CreateTempCbz();
        var outputPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        var errors = new List<string>();

        try
        {
            var window = new MainWindow(CreateUseCase(1, failGenerator: true));
            window.ShowError = (title, message) =>
            {
                errors.Add($"{title}: {message}");
                return Task.CompletedTask;
            };

            await window.SelectArchiveAsync(tempCbz);
            await window.ConvertAsync(outputPdf);

            Assert.Contains(errors, e => e.StartsWith("Conversion failed:"));
            Assert.Contains("failed", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.True(window.FindControl<Button>("OpenButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz);
            File.Delete(outputPdf);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_convert_keeps_items_in_queue_with_done_status()
    {
        var tempCbz = CreateTempCbz();
        var outputPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var window = new MainWindow(CreateUseCase(1));
            await window.SelectArchiveAsync(tempCbz);
            await window.ConvertAsync(outputPdf);

            var queue = window.FindControl<ItemsControl>("QueueList")!;
            Assert.True(queue.IsVisible);
            Assert.Equal(1, queue.Items?.Count);
            var item = Assert.IsType<MainWindow.QueueItem>(queue.Items![0]);
            Assert.Equal("done", item.Status);
            Assert.Contains("Done", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.False(window.FindControl<Button>("ConvertButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz);
            File.Delete(outputPdf);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_remove_archive_removes_item_from_queue()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz1 = CreateTempCbz();
        var tempCbz2 = CreateTempCbz();

        try
        {
            await window.SelectArchivesAsync([tempCbz1, tempCbz2]);

            window.RemoveArchiveFromQueue(tempCbz1);

            var queue = window.FindControl<ItemsControl>("QueueList")!;
            Assert.Equal(1, queue.Items?.Count);
            Assert.Equal("1 file queued",
                window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.True(window.FindControl<Button>("ConvertButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz1);
            File.Delete(tempCbz2);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_clear_queue_removes_all_items()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz1 = CreateTempCbz();
        var tempCbz2 = CreateTempCbz();

        try
        {
            await window.SelectArchivesAsync([tempCbz1, tempCbz2]);

            window.ClearQueue();

            var queue = window.FindControl<ItemsControl>("QueueList")!;
            Assert.Equal(0, queue.Items?.Count);
            Assert.False(window.FindControl<StackPanel>("QueuePanel")!.IsVisible);
            Assert.Equal("Ready", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.False(window.FindControl<Button>("ConvertButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz1);
            File.Delete(tempCbz2);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_queue_item_has_remove_button()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz = CreateTempCbz();

        try
        {
            await window.SelectArchiveAsync(tempCbz);

            window.Show();
            window.Measure(new Avalonia.Size(720, 520));
            window.Arrange(new Avalonia.Rect(0, 0, 720, 520));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            var queue = window.FindControl<ItemsControl>("QueueList")!;
            var container = queue.ContainerFromIndex(0);
            Assert.NotNull(container);
            Assert.NotNull(container!.FindDescendantOfType<Button>());
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    private static string CreateTempCbz()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        File.WriteAllBytes(path, [0x50, 0x4B, 0x05, 0x06]);
        return path;
    }

    private static ConvertComicUseCase CreateUseCase(int pageCount, bool failGenerator = false)
    {
        var pages = new ComicPage[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            pages[i] = new ComicPage
            {
                Number = i + 1,
                FileName = $"p{i + 1}.png",
                ImageData = [1],
            };
        }

        var archive = new ComicArchive { FilePath = "fake.cbz", Pages = pages };

        return new ConvertComicUseCase(
            new FakeReader(archive),
            failGenerator ? new ThrowingGenerator() : new FakeGenerator());
    }

    private sealed class FakeReader : IComicArchiveReader
    {
        private readonly ComicArchive _archive;

        public FakeReader(ComicArchive archive) => _archive = archive;

        public Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
            => Task.FromResult(_archive);
    }

    private sealed class FakeGenerator : IPdfGenerator
    {
        public Task GenerateAsync(
            ComicArchive archive,
            string outputPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < archive.Pages.Count; i++)
            {
                progress?.Report((int)Math.Round((i + 1) * 100.0 / archive.Pages.Count));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingGenerator : IPdfGenerator
    {
        public Task GenerateAsync(
            ComicArchive archive,
            string outputPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
            => throw new ComicArchiveException("Falha na geração.");
    }
}
