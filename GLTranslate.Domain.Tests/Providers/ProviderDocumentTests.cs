using GLTranslate.Abstractions.Providers;

namespace GLTranslate.Domain.Tests.Providers;

/// <summary>
/// Verifies what a document handed to a provider is made of and what it
/// refuses.
/// </summary>
public sealed class ProviderDocumentTests
{
    [Fact]
    public void Constructor_KeepsTheBytesAndTheName()
    {
        ProviderDocument document = new([1, 2, 3], "report.docx");

        Assert.Equal([1, 2, 3], document.Content.ToArray());
        Assert.Equal("report.docx", document.FileName);
    }

    [Theory]
    [InlineData("report.docx", "docx")]
    [InlineData("REPORT.PDF", "pdf")]
    [InlineData("archive.tar.gz", "gz")]
    [InlineData("README", "")]
    public void Format_IsTheLowercaseExtensionWithoutTheDot(string fileName, string expected)
    {
        Assert.Equal(expected, new ProviderDocument([1], fileName).Format);
    }

    [Fact]
    public void Constructor_CopiesTheBytes()
    {
        byte[] bytes = [1, 2, 3];
        ProviderDocument document = new(bytes, "a.txt");

        bytes[0] = 9;

        Assert.Equal(1, document.Content[0]);
    }

    [Fact]
    public void Constructor_EmptyContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ProviderDocument([], "a.txt"));
    }

    [Fact]
    public void Constructor_NullFileName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProviderDocument([1], null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankFileName_ThrowsArgumentException(string fileName)
    {
        Assert.Throws<ArgumentException>(() => new ProviderDocument([1], fileName));
    }
}
