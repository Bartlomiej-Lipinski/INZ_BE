using System.Security.Claims;

namespace Mates.Shared.Extensions;

public static class ClaimsPrincipalExtension
{
    public static string? GetUserId(this ClaimsPrincipal currentUser)
    {
        return currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? currentUser.FindFirst("sub")?.Value;
    }
}