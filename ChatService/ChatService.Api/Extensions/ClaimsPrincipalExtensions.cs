using System.Security.Claims;

namespace ChatService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the current user's ID from claims.
    /// Throws an exception if the user is not authenticated or claim is missing.
    /// </summary>
    /// <param name="user">ClaimsPrincipal</param>
    /// <returns>User ID as Guid</returns>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Try standard NameIdentifier claim first, then "sub"
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdString))
            throw new UnauthorizedAccessException("User ID claim is missing.");

        if (!Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("User ID claim is not a valid GUID.");

        return userId;
    }
}