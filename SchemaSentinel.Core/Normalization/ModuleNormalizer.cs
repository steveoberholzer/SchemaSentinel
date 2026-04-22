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

    private static readonly Regex InlineMultiSpace = new(
        @"[ \t]{2,}",
        RegexOptions.Compiled);

    // Matches /* ... */ block comments (non-greedy, dotall)
    private static readonly Regex BlockComments = new(
        @"/\*.*?\*/",
        RegexOptions.Singleline | RegexOptions.Compiled);

    // Matches -- line comments to end of line
    private static readonly Regex LineComments = new(
        @"--[^\n]*",
        RegexOptions.Compiled);

    // Strips bracket-quoting from identifiers: [name] → name
    private static readonly Regex BracketedIdentifier = new(
        @"\[(\w+)\]",
        RegexOptions.Compiled);

    public string Normalize(string definition, ComparisonOptions options)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return string.Empty;

        var result = definition.Replace("\r\n", "\n").Replace("\r", "\n");

        if (options.IgnoreSetStatements)
            result = SetStatementsPattern.Replace(result, string.Empty);

        if (options.IgnoreComments)
        {
            result = BlockComments.Replace(result, string.Empty);
            result = LineComments.Replace(result, string.Empty);
        }

        if (options.NormalizeBrackets)
            result = BracketedIdentifier.Replace(result, "$1");

        if (options.IgnoreWhitespace)
        {
            result = TrailingWhitespace.Replace(result, string.Empty);
            result = InlineMultiSpace.Replace(result, " ");
        }

        result = MultipleBlankLines.Replace(result, "\n\n");
        result = result.Trim();

        if (options.IgnoreCasing)
            result = result.ToUpperInvariant();

        return result;
    }
}
