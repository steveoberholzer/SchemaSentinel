using System.Text;

namespace SchemaSentinel.Core.Comparison;

public static class DiffEngine
{
    private const int MaxLcsLines = 3000;

    public static List<DiffLine> Compute(string? left, string? right)
    {
        var leftLines = Split(left);
        var rightLines = Split(right);

        if (leftLines.Length == 0 && rightLines.Length == 0)
            return new List<DiffLine>();

        var raw = leftLines.Length > MaxLcsLines || rightLines.Length > MaxLcsLines
            ? SimpleDiff(leftLines, rightLines)
            : LcsDiff(leftLines, rightLines);

        return MergeModified(raw);
    }

    private static string[] Split(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        return text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
    }

    private static List<DiffLine> LcsDiff(string[] left, string[] right)
    {
        int m = left.Length, n = right.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = m - 1; i >= 0; i--)
            for (int j = n - 1; j >= 0; j--)
                dp[i, j] = string.Equals(left[i], right[j], StringComparison.Ordinal)
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var result = new List<DiffLine>();
        int li = 0, ri = 0, leftNo = 1, rightNo = 1;

        while (li < m || ri < n)
        {
            if (li < m && ri < n && string.Equals(left[li], right[ri], StringComparison.Ordinal))
            {
                result.Add(new DiffLine(leftNo++, left[li], rightNo++, right[ri], DiffKind.Same));
                li++; ri++;
            }
            else if (ri < n && (li >= m || dp[li, ri + 1] > dp[li + 1, ri]))
            {
                result.Add(new DiffLine(null, string.Empty, rightNo++, right[ri], DiffKind.Added));
                ri++;
            }
            else
            {
                result.Add(new DiffLine(leftNo++, left[li], null, string.Empty, DiffKind.Removed));
                li++;
            }
        }

        return result;
    }

    private static List<DiffLine> SimpleDiff(string[] left, string[] right)
    {
        var result = new List<DiffLine>();
        int leftNo = 1, rightNo = 1;
        int maxLen = Math.Max(left.Length, right.Length);

        for (int i = 0; i < maxLen; i++)
        {
            bool hasLeft = i < left.Length;
            bool hasRight = i < right.Length;

            if (hasLeft && hasRight)
            {
                if (string.Equals(left[i], right[i], StringComparison.Ordinal))
                    result.Add(new DiffLine(leftNo++, left[i], rightNo++, right[i], DiffKind.Same));
                else
                {
                    result.Add(new DiffLine(leftNo++, left[i], null, string.Empty, DiffKind.Removed));
                    result.Add(new DiffLine(null, string.Empty, rightNo++, right[i], DiffKind.Added));
                }
            }
            else if (hasLeft)
                result.Add(new DiffLine(leftNo++, left[i], null, string.Empty, DiffKind.Removed));
            else
                result.Add(new DiffLine(null, string.Empty, rightNo++, right[i], DiffKind.Added));
        }

        return result;
    }

    // Merge consecutive Removed/Added blocks into Modified lines (paired in order).
    // Collects all interleaved Removed and Added lines in one pass so the pairing
    // is correct regardless of which order the LCS emits them.
    private static List<DiffLine> MergeModified(List<DiffLine> lines)
    {
        var result = new List<DiffLine>(lines.Count);
        int i = 0;

        while (i < lines.Count)
        {
            if (lines[i].Kind != DiffKind.Removed && lines[i].Kind != DiffKind.Added)
            {
                result.Add(lines[i++]);
                continue;
            }

            // Collect ALL consecutive Removed and Added in any interleaved order
            var removeds = new List<DiffLine>();
            var addeds  = new List<DiffLine>();
            while (i < lines.Count &&
                   (lines[i].Kind == DiffKind.Removed || lines[i].Kind == DiffKind.Added))
            {
                if (lines[i].Kind == DiffKind.Removed) removeds.Add(lines[i]);
                else                                    addeds.Add(lines[i]);
                i++;
            }

            int pairs = Math.Min(removeds.Count, addeds.Count);
            for (int p = 0; p < pairs; p++)
            {
                var (leftSegs, rightSegs) = CharDiff(removeds[p].LeftText!, addeds[p].RightText!);
                result.Add(new DiffLine(
                    removeds[p].LeftLineNo,  removeds[p].LeftText!,
                    addeds[p].RightLineNo,   addeds[p].RightText!,
                    DiffKind.Modified, leftSegs, rightSegs));
            }

            for (int p = pairs; p < removeds.Count; p++) result.Add(removeds[p]);
            for (int p = pairs; p < addeds.Count;   p++) result.Add(addeds[p]);
        }

        return result;
    }

    private static (IReadOnlyList<TextSegment> left, IReadOnlyList<TextSegment> right) CharDiff(string left, string right)
    {
        int m = left.Length, n = right.Length;

        // For very long lines skip char-level diff
        if (m * n > 40000)
            return ([new TextSegment(left, true)], [new TextSegment(right, true)]);

        var dp = new int[m + 1, n + 1];
        for (int i = m - 1; i >= 0; i--)
            for (int j = n - 1; j >= 0; j--)
                dp[i, j] = left[i] == right[j]
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var leftOps = new List<(char c, bool highlighted)>(m);
        var rightOps = new List<(char c, bool highlighted)>(n);
        int li = 0, ri = 0;

        while (li < m || ri < n)
        {
            if (li < m && ri < n && left[li] == right[ri])
            {
                leftOps.Add((left[li], false));
                rightOps.Add((right[ri], false));
                li++; ri++;
            }
            else if (ri < n && (li >= m || dp[li, ri + 1] > dp[li + 1, ri]))
            {
                rightOps.Add((right[ri], true));
                ri++;
            }
            else
            {
                leftOps.Add((left[li], true));
                li++;
            }
        }

        return (GroupOps(leftOps), GroupOps(rightOps));
    }

    private static IReadOnlyList<TextSegment> GroupOps(List<(char c, bool highlighted)> ops)
    {
        if (ops.Count == 0) return Array.Empty<TextSegment>();

        var segs = new List<TextSegment>();
        var buf = new StringBuilder();
        bool current = ops[0].highlighted;

        foreach (var (c, highlighted) in ops)
        {
            if (highlighted != current)
            {
                segs.Add(new TextSegment(buf.ToString(), current));
                buf.Clear();
                current = highlighted;
            }
            buf.Append(c);
        }

        if (buf.Length > 0)
            segs.Add(new TextSegment(buf.ToString(), current));

        return segs;
    }
}
