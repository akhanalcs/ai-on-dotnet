namespace RagChat.Web.Services.Ingestion;

// The smallest unit a reader produces: a paragraph (or heading/list block) plus where it came from.
//   Page    = PDF page number (null for Markdown)
//   Section = heading path, e.g. "3. Setup and Installation > 3.1 Charging" (null when unknown)
// Keeping provenance on every block is what lets a citation open the exact page later.
public sealed record DocumentBlock(string Text, int? Page, string? Section);
