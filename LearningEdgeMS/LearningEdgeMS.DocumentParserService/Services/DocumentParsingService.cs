using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using A = DocumentFormat.OpenXml.Drawing;

namespace LearningEdgeMS.DocumentParserService.Services;

public class DocumentParsingService
{
    public async Task<string> ExtractTextAsync(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".txt" => await File.ReadAllTextAsync(filePath),
            ".pdf" => ExtractPdf(filePath),
            ".docx" => ExtractDocx(filePath),
            ".pptx" => ExtractPptx(filePath),
            _ => throw new NotSupportedException($"Format {extension} is not supported.")
        };
    }

    private string ExtractPdf(string filePath)
    {
        var stringBuilder = new StringBuilder();

        using (PdfDocument document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                string pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    stringBuilder.AppendLine(pageText);
                }
            }
        }

        return stringBuilder.ToString();
    }

    private string ExtractDocx(string filePath)
    {
        var stringBuilder = new StringBuilder();

        // Open the Word document in read-only mode using OpenXML SDK
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
        {
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                // InnerText extracts all text content recursively from paragraphs and tables
                stringBuilder.Append(body.InnerText);
            }
        }

        return stringBuilder.ToString();
    }

    private string ExtractPptx(string filePath)
    {
        var stringBuilder = new StringBuilder();

        // Open the Presentation document in read-only mode using OpenXML SDK
        using (PresentationDocument presentationDoc = PresentationDocument.Open(filePath, false))
        {
            var presentationPart = presentationDoc.PresentationPart;
            if (presentationPart?.Presentation?.SlideIdList != null)
            {
                int slideNum = 1;
                foreach (var slideId in presentationPart.Presentation.SlideIdList.ChildElements.OfType<SlideId>())
                {
                    if (slideId.RelationshipId?.Value is string relId &&
                        presentationPart.GetPartById(relId) is SlidePart slidePart)
                    {
                        stringBuilder.AppendLine($"--- Slide {slideNum} ---");

                        // Extract text nodes from drawing ml elements within the slide
                        var texts = slidePart.Slide?.Descendants<A.Text>() ?? [];
                        foreach (var text in texts)
                        {
                            if (!string.IsNullOrEmpty(text.Text))
                            {
                                stringBuilder.Append(text.Text + " ");
                            }
                        }

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        slideNum++;
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }
}