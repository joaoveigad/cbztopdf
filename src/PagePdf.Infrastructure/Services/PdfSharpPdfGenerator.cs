using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PagePdf.Infrastructure.Services;

public sealed class PdfSharpPdfGenerator : IPdfGenerator
{
    public Task GenerateAsync(
        ComicArchive archive,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("O caminho de saída é obrigatório.", nameof(outputPath));
        }

        try
        {
            Generate(archive, outputPath, progress, cancellationToken);
            return Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ComicArchiveException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ComicArchiveException($"Falha ao gerar o PDF '{outputPath}': {ex.Message}", ex);
        }
    }

    private static void Generate(
        ComicArchive archive,
        string outputPath,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var images = new List<XImage>(archive.Pages.Count);
        var streams = new List<MemoryStream>(archive.Pages.Count); 

        try
        {
            using var document = new PdfDocument();
            document.Info.Title = Path.GetFileNameWithoutExtension(outputPath);

            for (var i = 0; i < archive.Pages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stream = new MemoryStream(archive.Pages[i].ImageData);
                streams.Add(stream);

                var image = XImage.FromStream(stream);
                images.Add(image);

                var pdfPage = document.AddPage();
                pdfPage.Width = XUnit.FromPoint(image.PixelWidth);
                pdfPage.Height = XUnit.FromPoint(image.PixelHeight);

                using (var graphics = XGraphics.FromPdfPage(pdfPage))
                {
                    graphics.DrawImage(image, 0, 0, image.PixelWidth, image.PixelHeight);
                }

                progress?.Report(i + 1);
            }

            cancellationToken.ThrowIfCancellationRequested();
            document.Save(outputPath);
        }
        finally
        {
            foreach (var image in images)
            {
                image.Dispose();
            }

            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }
    }
}
