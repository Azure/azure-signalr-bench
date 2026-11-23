#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace Portal.Mcp
{
    /// <summary>
    /// Hosts the perf MCP server at <see cref="Path"/> as an OAuth 2.0 protected resource (MCP authorization spec):
    /// clients sign the user in with Microsoft Entra ID and call it with a delegated access token for the portal app.
    /// </summary>
    public static class PerfMcpExtensions
    {
        public const string Path = "/mcp";

        public const string InstallPath = Path + "/install";

        // Delegated permission exposed by the portal app registration, see Initialize/enable_mcp.sh.
        public const string Scope = "user_impersonation";

        public const string PolicyName = "PerfMcp";

        private const string ServerInstructions =
            "Perf portal for Azure SignalR and Web PubSub benchmarks. When the user asks why a test run failed, " +
            "is Suspicious or did not meet thresholds: read the run with get_test_run, call get_troubleshooting_guide " +
            "without a path for the index, open the symptom document that matches the run's ErrorInfo, Check and report, " +
            "and follow its links into the debugging documents. Base conclusions on evidence and say which checks you could not run.";

        public static IServiceCollection AddPerfMcp(this IServiceCollection services, IConfiguration configuration)
        {
            var azureAd = configuration.GetSection("AzureAd");
            var instance = azureAd["Instance"]!.TrimEnd('/');
            var tenantId = azureAd["TenantId"];

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApi(azureAd, JwtBearerDefaults.AuthenticationScheme);
            services.AddAuthentication()
                .AddMcp(options =>
                {
                    options.ResourceMetadata = new ProtectedResourceMetadata
                    {
                        AuthorizationServers = { $"{instance}/{tenantId}/v2.0" },
                    };
                    options.Events.OnResourceMetadataRequest = context =>
                    {
                        var metadata = context.ResourceMetadata
                            ?? throw new InvalidOperationException("MCP resource metadata is unavailable.");
                        if (string.IsNullOrWhiteSpace(metadata.Resource))
                            throw new InvalidOperationException("MCP resource metadata has no canonical resource URI.");

                        metadata.ScopesSupported.Clear();
                        metadata.ScopesSupported.Add($"{metadata.Resource}/{Scope}");
                        return Task.CompletedTask;
                    };
                });
            // A bearer challenge must point the client to the protected resource metadata, not to the web sign-in.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme);

            services.AddAuthorization(options => options.AddPolicy(PolicyName, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(PerfConstants.Roles.Contributor, PerfConstants.Roles.Reader)
                .RequireAssertion(context => HasScope(context.User, Scope))));

            services.AddMcpServer(options => options.ServerInstructions = ServerInstructions)
                .WithHttpTransport(options => options.Stateless = true)
                .AddAuthorizationFilters()
                .WithTools<PerfMcpTools>();
            return services;
        }

        public static IEndpointConventionBuilder MapPerfMcp(this IEndpointRouteBuilder endpoints)
        {
            // Anonymous so a user's agent can fetch it before the MCP server is installed; it only reveals the MCP URL.
            endpoints.MapGet(InstallPath, (HttpContext context) => Results.Text(
                    McpInstallPage.Render($"{context.Request.Scheme}://{context.Request.Host}{Path}"), "text/plain; charset=utf-8"))
                .AllowAnonymous();
            return endpoints.MapMcp(Path).RequireAuthorization(PolicyName);
        }

        // Only delegated (user) tokens are accepted; app-only tokens carry no scope.
        private static bool HasScope(ClaimsPrincipal user, string scope)
        {
            return user.FindAll("scp")
                .Concat(user.FindAll(ClaimConstants.Scope))
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(scope, StringComparer.Ordinal);
        }
    }
}
