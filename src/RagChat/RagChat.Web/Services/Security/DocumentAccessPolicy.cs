namespace RagChat.Web.Services.Security;

// Who may read which document (the "ethical wall"), read from the "DocumentAccess" config section:
//   "DocumentAccess": { "Example_GPS_Watch.md": [ "COC-4" ] }
// Default deny: a document that isn't listed is visible to no one.
// In production this comes from the source system's ACLs (DMS, SharePoint, ServiceNow), not config.
public sealed class DocumentAccessPolicy(IConfiguration configuration)
{
    private readonly Dictionary<string, string[]> _groupsByDocument =
        configuration.GetSection("DocumentAccess").Get<Dictionary<string, string[]>>() ?? [];

    public string[] GroupsFor(string documentId) => _groupsByDocument.GetValueOrDefault(documentId, []);

    public bool CanRead(IReadOnlyCollection<string> userGroups, string documentId) =>
        GroupsFor(documentId).Intersect(userGroups).Any();
}
