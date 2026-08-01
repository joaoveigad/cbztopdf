using NaturalSort.Extension;
using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using System.IO.Compression;

namespace PagePdf.Infrastructure.Services;

public sealed class ZipComicArchiveReader : IComicArchiveReader
{
    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];

    public async Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            throw new ArgumentException("O caminho do arquivo .cbz é obrigatório.", nameof(archivePath));
        }

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Arquivo não encontrado: {archivePath}", archivePath);
        }

        try
        {
            using var archive = ZipFile.OpenRead(archivePath);

            var entries = archive.Entries
                .Where(IsImageEntry)
                .OrderBy(e => e.FullName, StringComparison.OrdinalIgnoreCase.WithNaturalSort())
                .ToList();

            if (entries.Count == 0)
            {
                throw new ComicArchiveException($"Nenhuma imagem encontrada em '{archivePath}'.");
            }

            var pages = new List<ComicPage>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var stream = entries[i].Open();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, cancellationToken);

                pages.Add(new ComicPage
                {
                    Number = i + 1,
                    FileName = entries[i].FullName,
                    ImageData = memory.ToArray(),
                });
            }

            return new ComicArchive { FilePath = archivePath, Pages = pages };
        }
        catch (ComicArchiveException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ComicArchiveException($"Falha ao ler o arquivo '{archivePath}': {ex.Message}", ex);
        }
    }

    private static bool IsImageEntry(ZipArchiveEntry entry)
    {
        var name = entry.Name;
        return name.Length > 0
            && !name.StartsWith('.')
            && !name.StartsWith("._", StringComparison.Ordinal)
            && SupportedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
    }
}
