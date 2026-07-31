using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;

namespace PagePdf.Infrastructure.Services;

public sealed class PdfSharpPdfGenerator : IPdfGenerator
{
    public Task GenerateAsync(ComicArchive archive, string outputPath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
