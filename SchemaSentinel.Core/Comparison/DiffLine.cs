namespace SchemaSentinel.Core.Comparison;

public enum DiffKind { Same, Removed, Added, Modified }

public record TextSegment(string Text, bool IsHighlighted);

public record DiffLine(
    int? LeftLineNo, string LeftText,
    int? RightLineNo, string RightText,
    DiffKind Kind,
    IReadOnlyList<TextSegment>? LeftSegments = null,
    IReadOnlyList<TextSegment>? RightSegments = null);
