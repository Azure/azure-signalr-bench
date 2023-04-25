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
}