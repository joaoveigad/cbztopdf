using PagePdf.Domain.Exceptions;
using PagePdf.Infrastructure.Services;
using System.IO.Compression;
using Xunit;

namespace PagePdf.Infrastructure.Tests;

public class ZipComicArchiveReaderTests
{
    private readonly ZipComicArchiveReader _reader = new();

    [Fact]
    public async Task ReadAsync_returns_pages_in_natural_order()
    {
        var path = CreateTempCbz(
            ("page10.png", new byte[] { 1 }),
            ("page2.png", new byte[] { 2 }),
            ("page1.png", new byte[] { 3 }));

        try
        {
            var archive = await _reader.ReadAsync(path);

            Assert.Equal(path, archive.FilePath);
            Assert.Equal(3, archive.PageCount);
            Assert.Equal(new[] { "page1.png", "page2.png", "page10.png" }, archive.Pages.Select(p => p.FileName));
            Assert.Equal(new[] { 1, 2, 3 }, archive.Pages.Select(p => p.Number));
            Assert.Equal(new byte[] { 3 }, archive.Pages[0].ImageData);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_filters_out_non_images_and_metadata()
    {
        var path = CreateTempCbz(
            ("1.jpg", new byte[] { 1 }),
            ("cover.jpeg", new byte[] { 2 }),
            ("notes.txt", new byte[] { 3 }),
            ("__MACOSX/._1.jpg", new byte[] { 4 }),
            (".hidden.png", new byte[] { 5 }),
            ("folder/2.bmp", new byte[] { 6 }),
            ("3.webp", new byte[] { 7 }));

        try
        {
            var archive = await _reader.ReadAsync(path);

            Assert.Equal(4, archive.PageCount);
            Assert.Equal(
                new[] { "1.jpg", "3.webp", "cover.jpeg", "folder/2.bmp" },
                archive.Pages.Select(p => p.FileName));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_throws_when_archive_has_no_images()
    {
        var path = CreateTempCbz(("readme.txt", new byte[] { 1 }));

        try
        {
            var ex = await Assert.ThrowsAsync<ComicArchiveException>(() => _reader.ReadAsync(path));
            Assert.Contains("Nenhuma imagem", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_throws_when_file_does_not_exist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");

        await Assert.ThrowsAsync<FileNotFoundException>(() => _reader.ReadAsync(path));
    }

    [Fact]
    public async Task ReadAsync_throws_when_file_is_not_a_zip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        File.WriteAllBytes(path, [1, 2, 3, 4]);

        try
        {
            var ex = await Assert.ThrowsAsync<ComicArchiveException>(() => _reader.ReadAsync(path));
            Assert.Contains("Falha ao ler", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempCbz(params (string FileName, byte[] Data)[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        foreach (var (fileName, data) in entries)
        {
            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            entryStream.Write(data);
        }

        return path;
    }
}
