namespace SchemaSentinel.Core.Models;

public enum ModuleType { View, Procedure, Function }

public class SqlModuleModel
{
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public ModuleType ModuleType { get; set; }
    public string Definition { get; set; } = string.Empty;
    public DateTime? ModifyDate { get; set; }
    public List<ParameterModel> Parameters { get; set; } = new();

    public string FullName => $"{SchemaName}.{ObjectName}";
}

public class ParameterModel
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsOutput { get; set; }
    public int OrdinalPosition { get; set; }
}
