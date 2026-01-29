namespace Reeb.SqlOM;

/// <summary>
/// Specifies the database table name for an entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableNameAttribute : Attribute
{
    public string Name { get; }

    public TableNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Specifies the default alias for a table in queries.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAliasAttribute : Attribute
{
    public string Alias { get; }

    public TableAliasAttribute(string alias)
    {
        Alias = alias;
    }
}

/// <summary>
/// Specifies that a property maps to a different column name in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnNameAttribute : Attribute
{
    public string Name { get; }

    public ColumnNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Specifies that a property should be ignored when building queries.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IgnoreColumnAttribute : Attribute
{
}
