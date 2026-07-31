using PagePdf.Application.DTOs;
using PagePdf.Application.Tests.Fakes;
using PagePdf.Application.UseCases;
using PagePdf.Domain.Entities;
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
            Assert.Equal(new[] { 1, 2, 3 }, reported);
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

    private static string CreateTempCbzFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        File.WriteAllBytes(path, [0x50, 0x4B, 0x05, 0x06]);
        return path;
    }
}
