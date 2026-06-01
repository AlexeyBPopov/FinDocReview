using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FinDocReview.Infrastructure.Services;

public class TextExtractionService
{
    public Task<string> ExtractTextAsync(string filePath, string contentType)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        var text = ext switch
        {
            ".pdf" => ExtractFromPdf(filePath),
            ".txt" => ExtractFromTxt(filePath),
            ".docx" => ExtractFromDocx(filePath),
            _ => throw new NotSupportedException($"File type '{ext}' is not supported.")
        };

        return Task.FromResult(text);
    }

    private static string ExtractFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (Page page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractFromTxt(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    private static string ExtractFromDocx(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;
        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }
}