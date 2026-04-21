using System.Text.RegularExpressions;
using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Core.Normalization;

public class ModuleNormalizer
{
    private static readonly Regex SetStatementsPattern = new(
        @"^\s*SET\s+(ANSI_NULLS|QUOTED_IDENTIFIER)\s+(ON|OFF)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex MultipleBlankLines = new(
        @"(\r?\n){3,}",
        RegexOptions.Compiled);

    private static readonly Regex TrailingWhitespace = new(
        @"[ \t]+$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public string Normalize(string definition, ComparisonOptions options)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return string.Empty;

        var result = definition.Replace("\r\n", "\n").Replace("\r", "\n");

        if (options.IgnoreSetStatements)
            result = SetStatementsPattern.Replace(result, string.Empty);

        if (options.IgnoreWhitespace)
            result = TrailingWhitespace.Replace(result, string.Empty);

        result = MultipleBlankLines.Replace(result, "\n\n");
        result = result.Trim();

        if (options.IgnoreCasing)
            result = result.ToUpperInvariant();

        return result;
    }
}
