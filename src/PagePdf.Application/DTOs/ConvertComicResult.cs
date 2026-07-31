namespace PagePdf.Application.DTOs;

public sealed record ConvertComicResult(string OutputPath, int PageCount, TimeSpan Elapsed);
