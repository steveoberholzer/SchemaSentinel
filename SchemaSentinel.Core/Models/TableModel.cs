namespace SchemaSentinel.Core.Models;

public class TableModel
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime? ModifyDate { get; set; }
    public List<ColumnModel> Columns { get; set; } = new();
    public PrimaryKeyModel? PrimaryKey { get; set; }

    public string FullName => $"{SchemaName}.{TableName}";
}

public class PrimaryKeyModel
{
    public string ConstraintName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsClustered { get; set; }
}
