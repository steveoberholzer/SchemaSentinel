namespace SchemaSentinel.Core.Models;

public class ColumnModel
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public int? IdentitySeed { get; set; }
    public int? IdentityIncrement { get; set; }
    public bool IsComputed { get; set; }
    public string? ComputedDefinition { get; set; }
    public string? DefaultDefinition { get; set; }
    public int OrdinalPosition { get; set; }
}
