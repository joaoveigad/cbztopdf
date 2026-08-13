using PagePdf.Domain.Entities;
using Xunit;

namespace PagePdf.Domain.Tests;

public class ComicArchiveTests
{
    [Fact]
    public void PageCount_derives_from_pages_count()
    {
        var archive = new ComicArchive
        {
            FilePath = "comic.cbz",
            Pages =
            [
                new ComicPage { Number = 1, FileName = "p1.jpg", ImageData = [1] },
                new ComicPage { Number = 2, FileName = "p2.jpg", ImageData = [2] },
                new ComicPage { Number = 3, FileName = "p3.jpg", ImageData = [3] },
            ],
        };

        Assert.Equal(3, archive.PageCount);
        Assert.Equal(archive.Pages.Count, archive.PageCount);
    }

    [Fact]
    public void PageCount_is_zero_when_pages_is_empty()
    {
        var archive = new ComicArchive
        {
            FilePath = "comic.cbz",
            Pages = [],
        };

        Assert.Equal(0, archive.PageCount);
    }

    [Fact]
    public void FilePath_is_preserved()
    {
        var archive = new ComicArchive
        {
            FilePath = "some/folder/comic.cbz",
            Pages = [],
        };

        Assert.Equal("some/folder/comic.cbz", archive.FilePath);
    }
}
