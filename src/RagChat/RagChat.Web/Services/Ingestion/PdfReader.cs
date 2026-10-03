using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace RagChat.Web.Services.Ingestion;

// Reads a PDF page by page with PdfPig and splits each page into text blocks (paragraphs) using layout analysis.
// Every block keeps its page number. In production, Azure AI Document Intelligence does this too,
// plus OCR for scanned PDFs and table structure.
public static class PdfReader
{
    public static IEnumerable<DocumentBlock> Read(string path)
    {
        using var pdf = PdfDocument.Open(path);
        foreach (var page in pdf.GetPages())
        {
            // letters -> words -> blocks (Docstrum groups words into paragraphs by their position on the page)
            var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
            foreach (var block in DocstrumBoundingBoxes.Instance.GetBlocks(words))
            {
                var text = string.Join(' ', block.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                if (text.Length > 0)
                {
                    yield return new DocumentBlock(text, page.Number, Section: null);
                }
            }
        }
    }
}
