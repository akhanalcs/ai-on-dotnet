using System.Security.Claims;

namespace RagChat.Web.Services.Security;

public static class UserGroups
{
    // The user's access groups = the Entra ID app roles in their token ("roles" claim), e.g. ["COC-6"].
    // App roles (not raw group IDs) keep tokens small and readable, and avoid the 200-group "overage" limit.
    public static IReadOnlyCollection<string> From(ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type is "roles" or ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToArray();
}
