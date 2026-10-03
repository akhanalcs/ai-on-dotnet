using Microsoft.AspNetCore.StaticFiles;

namespace RagChat.Web.Services.Security;

public static class DocumentEndpoints
{
    // Source files are served only to signed-in users allowed to read them (citation links open these URLs).
    // Without this, trimming search results is not enough: anyone could open /Data/<file> directly.
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var contentTypes = new FileExtensionContentTypeProvider();

        app.MapGet("/documents/{name}", (
            string name,
            HttpContext http,
            DocumentAccessPolicy policy,
            [FromKeyedServices("ingestion_directory")] DirectoryInfo documents) =>
        {
            var fileName = Path.GetFileName(name); // strips any "../" so callers can't escape the folder
            var path = Path.Combine(documents.FullName, fileName);

            // 404 (not 403) when not allowed, so we don't reveal that the document exists
            if (!policy.CanRead(UserGroups.From(http.User), fileName) || !File.Exists(path))
            {
                return Results.NotFound();
            }

            var contentType = contentTypes.TryGetContentType(fileName, out var type) ? type : "application/octet-stream";
            return Results.File(path, contentType);
        }).RequireAuthorization();

        return app;
    }
}
