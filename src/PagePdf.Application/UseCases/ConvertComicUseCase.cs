using PagePdf.Application.DTOs;
using PagePdf.Application.Interfaces;

namespace PagePdf.Application.UseCases;

public sealed class ConvertComicUseCase
{
    private readonly IComicArchiveReader _archiveReader;
    private readonly IPdfGenerator _pdfGenerator;

    public ConvertComicUseCase(IComicArchiveReader archiveReader, IPdfGenerator pdfGenerator)
    {
        _archiveReader = archiveReader;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<ConvertComicResult> ExecuteAsync(
        ConvertComicRequest request,
        CancellationToken cancellationToken = default)
    {
        var archive = await _archiveReader.ReadAsync(request.ArchivePath, cancellationToken);
        await _pdfGenerator.GenerateAsync(archive, request.OutputPath, cancellationToken);
        return new ConvertComicResult(request.OutputPath, archive.PageCount, TimeSpan.Zero);
    }
}
