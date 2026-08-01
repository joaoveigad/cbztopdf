using PagePdf.Application.DTOs;
using PagePdf.Application.Interfaces;
using PagePdf.Application.Tests.Fakes;
using PagePdf.Application.UseCases;
using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using Xunit;

namespace PagePdf.Application.Tests;

public class ConvertComicUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_reports_progress_for_each_page()
    {
        var archive = new ComicArchive
        {
            FilePath = "fake.cbz",
            Pages = new[]
            {
                new ComicPage { Number = 1, FileName = "p1.jpg", ImageData = [1] },
                new ComicPage { Number = 2, FileName = "p2.jpg", ImageData = [2] },
                new ComicPage { Number = 3, FileName = "p3.jpg", ImageData = [3] },
            },
        };

        var tempCbz = CreateTempCbzFile();
        try
        {
            var reader = new FakeComicArchiveReader(archive);
            var generator = new FakePdfGenerator();
            var useCase = new ConvertComicUseCase(reader, generator);

            var reported = new List<int>();
            var result = await useCase.ExecuteAsync(
                new ConvertComicRequest(tempCbz, "out.pdf"),
                new SynchronousProgress<int>(reported.Add));

            Assert.Equal(3, result.PageCount);
            Assert.Equal(3, generator.ReportCount);
            Assert.Equal(new[] { 33, 67, 100 }, reported);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [Fact]
    public async Task ExecuteAsync_without_progress_does_not_throw()
    {
        var archive = new ComicArchive
        {
            FilePath = "fake.cbz",
            Pages = new[]
            {
                new ComicPage { Number = 1, FileName = "p1.jpg", ImageData = [1] },
            },
        };

        var tempCbz = CreateTempCbzFile();
        try
        {
            var reader = new FakeComicArchiveReader(archive);
            var generator = new FakePdfGenerator();
            var useCase = new ConvertComicUseCase(reader, generator);

            var result = await useCase.ExecuteAsync(new ConvertComicRequest(tempCbz, "out.pdf"));

            Assert.Equal(1, result.PageCount);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [Fact]
    public async Task ExecuteAsync_returns_output_path_and_elapsed()
    {
        var archive = new ComicArchive
        {
            FilePath = "fake.cbz",
            Pages = new[]
            {
                new ComicPage { Number = 1, FileName = "p1.jpg", ImageData = [1] },
                new ComicPage { Number = 2, FileName = "p2.jpg", ImageData = [2] },
            },
        };

        var tempCbz = CreateTempCbzFile();
        const string outputPath = "out.pdf";
        try
        {
            var useCase = new ConvertComicUseCase(
                new FakeComicArchiveReader(archive),
                new FakePdfGenerator());

            var result = await useCase.ExecuteAsync(new ConvertComicRequest(tempCbz, outputPath));

            Assert.Equal(outputPath, result.OutputPath);
            Assert.Equal(2, result.PageCount);
            Assert.True(result.Elapsed > TimeSpan.Zero);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [Fact]
    public async Task ExecuteAsync_propagates_reader_error()
    {
        var tempCbz = CreateTempCbzFile();
        try
        {
            var useCase = new ConvertComicUseCase(
                new ThrowingComicArchiveReader(),
                new FakePdfGenerator());

            await Assert.ThrowsAsync<ComicArchiveException>(
                () => useCase.ExecuteAsync(new ConvertComicRequest(tempCbz, "out.pdf")));
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [Fact]
    public async Task ExecuteAsync_propagates_generator_error()
    {
        var archive = new ComicArchive
        {
            FilePath = "fake.cbz",
            Pages = new[]
            {
                new ComicPage { Number = 1, FileName = "p1.jpg", ImageData = [1] },
            },
        };

        var tempCbz = CreateTempCbzFile();
        try
        {
            var useCase = new ConvertComicUseCase(
                new FakeComicArchiveReader(archive),
                new ThrowingPdfGenerator());

            await Assert.ThrowsAsync<ComicArchiveException>(
                () => useCase.ExecuteAsync(new ConvertComicRequest(tempCbz, "out.pdf")));
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [Fact]
    public async Task ExecuteAsync_rethrows_validation_error_before_reading()
    {
        var useCase = new ConvertComicUseCase(
            new FakeComicArchiveReader(new ComicArchive
            {
                FilePath = "fake.cbz",
                Pages = Array.Empty<ComicPage>(),
            }),
            new FakePdfGenerator());

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => useCase.ExecuteAsync(new ConvertComicRequest(
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz"),
                "out.pdf")));
    }

    private static string CreateTempCbzFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        File.WriteAllBytes(path, [0x50, 0x4B, 0x05, 0x06]);
        return path;
    }

    private sealed class ThrowingComicArchiveReader : IComicArchiveReader
    {
        public Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
            => throw new ComicArchiveException("Falha na leitura.");
    }

    private sealed class ThrowingPdfGenerator : IPdfGenerator
    {
        public Task GenerateAsync(
            ComicArchive archive,
            string outputPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
            => throw new ComicArchiveException("Falha na geração.");
    }
}
