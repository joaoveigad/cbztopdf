using PagePdf.Application.DTOs;
using PagePdf.Application.Interfaces;
using PagePdf.Domain.Exceptions;
using System.Diagnostics;

namespace PagePdf.Application.UseCases;

public sealed class ConvertComicUseCase
{
    private static readonly string[] SupportedExtensions = [".cbz"];

    private readonly IComicArchiveReader _archiveReader;
    private readonly IPdfGenerator _pdfGenerator;

    public ConvertComicUseCase(IComicArchiveReader archiveReader, IPdfGenerator pdfGenerator)
    {
        _archiveReader = archiveReader;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<ConvertComicResult> ExecuteAsync(
        ConvertComicRequest request,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var archive = await _archiveReader.ReadAsync(request.ArchivePath, cancellationToken);  // src/PagePdf.Infrastructure/Services/ZipComicArchiveReader.cs
            await _pdfGenerator.GenerateAsync(archive, request.OutputPath, progress, cancellationToken); //src/PagePdf.Infrastructure/Services/PdfSharpPdfGenerator.cs
            stopwatch.Stop();
            return new ConvertComicResult(request.OutputPath, archive.PageCount, stopwatch.Elapsed);
        }
        catch (ComicArchiveException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ComicArchiveException($"Falha ao converter '{request.ArchivePath}': {ex.Message}", ex);
        }
    }

    private static void Validate(ConvertComicRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ArchivePath))
        {
            throw new ArgumentException("O caminho do arquivo .cbz é obrigatório.", nameof(request.ArchivePath));
        }

        if (!File.Exists(request.ArchivePath))
        {
            throw new FileNotFoundException($"Arquivo não encontrado: {request.ArchivePath}", request.ArchivePath);
        }

        var extension = Path.GetExtension(request.ArchivePath).ToLowerInvariant();

        var supported = false;
        for (var i = 0; i < SupportedExtensions.Length; i++)
        {
            if (SupportedExtensions[i] == extension)
            {
                supported = true;
                break;
            }
        }

        if (!supported)
        {
            throw new ComicArchiveException(
                $"Extensão '{extension}' não suportada. Use: {string.Join(" ou ", SupportedExtensions)}");
        }
    }
}
