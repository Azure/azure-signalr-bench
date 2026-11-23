#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Portal.Mcp
{
    /// <summary>
    /// The repository's tsg/ Markdown documents, embedded into the portal assembly at build time (see Portal.csproj),
    /// so the deployed portal always serves the guides it was released with.
    /// </summary>
    public static class TroubleshootingGuides
    {
        public const string IndexPath = "README.md";

        public const string SourceBaseUrl = "https://github.com/Azure/azure-signalr-bench/blob/v2/";

        private const string ResourcePrefix = "tsg/";

        private static readonly Dictionary<string, string> Documents = Load();

        public static IReadOnlyList<string> Paths { get; } = Documents.Keys.OrderBy(p => p, StringComparer.Ordinal).ToList();

        /// <summary>
        /// Reads a document by its path under tsg/. Accepts the relative links used inside the documents,
        /// e.g. "../debugging/aks-logs-and-nodes.md#anchor" or "./symptoms/high-latency.md".
        /// </summary>
        public static bool TryRead(string path, out string normalizedPath, out string content)
        {
            var requested = Normalize(path);
            var key = Documents.Keys.FirstOrDefault(k => string.Equals(k, requested, StringComparison.OrdinalIgnoreCase));
            if (key == null)
            {
                normalizedPath = requested;
                content = string.Empty;
                return false;
            }

            normalizedPath = key;
            content = Documents[key];
            return true;
        }

        private static string Normalize(string path)
        {
            var withoutAnchor = path.Split('#')[0].Replace('\\', '/');
            var segments = withoutAnchor.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s != "." && s != "..")
                .ToList();
            if (segments.Count > 0 && string.Equals(segments[0], "tsg", StringComparison.OrdinalIgnoreCase))
            {
                segments.RemoveAt(0);
            }

            return string.Join('/', segments);
        }

        private static Dictionary<string, string> Load()
        {
            var assembly = typeof(TroubleshootingGuides).Assembly;
            var documents = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in assembly.GetManifestResourceNames())
            {
                var path = name.Replace('\\', '/');
                if (!path.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                documents[path.Substring(ResourcePrefix.Length)] = reader.ReadToEnd();
            }

            return documents;
        }
    }
}
