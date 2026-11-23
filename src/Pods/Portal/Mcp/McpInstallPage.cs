#nullable enable

namespace Portal.Mcp
{
    /// <summary>
    /// Plain-text page at /mcp/install with a prompt users can hand to their AI agent to install the perf MCP server.
    /// </summary>
    public static class McpInstallPage
    {
        public static string Render(string mcpUrl) => $$"""
            # Install the perf MCP server

            Copy everything between the lines below and send it to your AI agent
            (GitHub Copilot in VS Code, Copilot CLI, Claude Code, ...).

            ------------------------------------------------------------------------
            Install the Azure SignalR perf MCP server for me.

            - Server name: perf
            - Transport: HTTP (streamable HTTP)
            - URL: {{mcpUrl}}
            - Authentication: OAuth 2.0 with Microsoft Entra ID, discovered from the server's
              protected resource metadata. Do not configure any static token, API key or header.

            Steps:
            1. Add the server to the MCP configuration of the client you are running in, at user
               level unless I ask for a specific workspace. Keep any existing servers. In VS Code,
               add it to mcp.json:
                 { "servers": { "perf": { "type": "http", "url": "{{mcpUrl}}" } } }
               For other clients, use that client's own MCP config file or CLI command.
            2. Start (or reload) the server and ask me to complete the Microsoft sign-in when
               prompted. I need the perf Reader role to read, or Contributor to change tests.
               If your client cannot complete the Entra sign-in, stop and tell me; do not work
               around authentication.
            3. Verify by calling the get_perf_info tool and show me my user name and roles.
            4. Tell me I can now ask things like "why did test <name> run <index> fail?"; the
               server's get_troubleshooting_guide tool provides the troubleshooting guides.
            ------------------------------------------------------------------------
            """;
    }
}
