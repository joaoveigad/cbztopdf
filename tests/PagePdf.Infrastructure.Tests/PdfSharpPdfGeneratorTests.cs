using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using PagePdf.Infrastructure.Services;
using Xunit;

namespace PagePdf.Infrastructure.Tests;

public class PdfSharpPdfGeneratorTests
{
    private static readonly byte[] OnePixelPng =
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly PdfSharpPdfGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_creates_pdf_with_pdf_signature()
    {
        var archive = CreateArchive(3);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            await _generator.GenerateAsync(archive, outputPath);

            var bytes = File.ReadAllBytes(outputPath);
            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_creates_parent_directory()
    {
        var archive = CreateArchive(1);
        var directory = Path.Combine(Path.GetTempPath(), $"pagepdf_{Guid.NewGuid():N}");
        var outputPath = Path.Combine(directory, "out.pdf");

        try
        {
            await _generator.GenerateAsync(archive, outputPath);

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_reports_progress_for_each_page()
    {
        var archive = CreateArchive(3);
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var reported = new List<int>();
            var progress = new SynchronousProgress<int>(reported.Add);

            await _generator.GenerateAsync(archive, outputPath, progress);

            Assert.Equal(new[] { 33, 67, 100 }, reported);
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_throws_when_image_data_is_invalid()
    {
        var archive = new ComicArchive
        {
            FilePath = "fake.cbz",
            Pages = new[]
            {
                new ComicPage { Number = 1, FileName = "p1.png", ImageData = [1, 2, 3] },
            },
        };
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var ex = await Assert.ThrowsAsync<ComicArchiveException>(
                () => _generator.GenerateAsync(archive, outputPath));

            Assert.Contains("Falha ao gerar", ex.Message);
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_throws_when_output_path_is_blank()
    {
        var archive = CreateArchive(1);

        await Assert.ThrowsAsync<ArgumentException>(() => _generator.GenerateAsync(archive, " "));
    }

    private static ComicArchive CreateArchive(int pageCount)
    {
        var pages = new ComicPage[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            pages[i] = new ComicPage
            {
                Number = i + 1,
                FileName = $"p{i + 1}.png",
                ImageData = OnePixelPng,
            };
        }

        return new ComicArchive { FilePath = "fake.cbz", Pages = pages };
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value) => _handler(value);
    }
}
