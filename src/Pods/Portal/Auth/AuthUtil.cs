#nullable enable
using System.Security.Claims;
using Azure.SignalRBench.Common;

namespace Portal.Auth;

public static class AuthUtil
{
    public static bool CanReadSecret(ClaimsPrincipal user)
    {
       return user.IsInRole(PerfConstants.Roles.Contributor);
    }
    
    public static bool CanAccessCluster(ClaimsPrincipal user)
    {
        return user.IsInRole(PerfConstants.Roles.Contributor) ;
    }

    // Web sign-in sets Identity.Name; v2 Entra access tokens carry the user in preferred_username, v1 in upn.
    public static string? GetUserName(ClaimsPrincipal user)
    {
        return user.Identity?.Name
               ?? user.FindFirst("preferred_username")?.Value
               ?? user.FindFirst(ClaimTypes.Upn)?.Value
               ?? user.FindFirst("upn")?.Value;
    }
}