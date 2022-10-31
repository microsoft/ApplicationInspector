using System;
using System.Text.RegularExpressions;

namespace BlazorSarifViewer.Wasm
{
    public static class GitHubUrlConverter
    {
        private static Regex expression = new Regex("https://github.com/(.*)/(.*).git");
        public static bool TryParseGitHubUrl(Uri uri, out string? owner, out string? repository)
        {
            (owner, repository) = (null, null);
            var matches = expression.Match(uri.ToString());
            if (matches.Success)
            {
                (owner, repository) = (matches.Groups[1].Captures[0].Value, matches.Groups[2].Captures[0].Value);
                return true;
            }
            return false;
        }
    }
}
