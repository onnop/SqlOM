using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM;

public static class SqlOMExtensions
{
    #region SqlConstantCollection Extensions

    public static SqlConstantCollection ToSqlConstantCollection(this List<Guid> values)
    {
        var collection = new SqlConstantCollection();
        foreach (var value in values)
        {
            collection.Add(SqlConstant.Guid(value));
        }
        return collection;
    }

    #endregion

    #region Table/Column Name Helpers

    /// <summary>
    /// Gets the table name for a type, using [TableName] attribute or type name.
    /// </summary>
    public static string TableName<T>()
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<TableNameAttribute>();
        return attr?.Name ?? type.Name;
    }

    /// <summary>
    /// Gets the table alias for a type, using [TableAlias] attribute or first letter.
    /// </summary>
    public static string TableAlias<T>()
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<TableAliasAttribute>();
        return attr?.Alias ?? type.Name[..1].ToLowerInvariant();
    }

    /// <summary>
    /// Creates a FromTerm for a type using its [TableName] and [TableAlias] attributes.
    /// </summary>
    public static FromTerm Table<T>()
    {
        return FromTerm.Table(TableName<T>(), TableAlias<T>());
    }

    /// <summary>
    /// Creates a FromTerm for a type with a custom alias.
    /// </summary>
    public static FromTerm Table<T>(string alias)
    {
        return FromTerm.Table(TableName<T>(), alias);
    }

    /// <summary>
    /// Gets the column name for a property, using [ColumnName] attribute or property name.
    /// </summary>
    public static string ColumnName<T>(Expression<Func<T, object?>> expression)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var attr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        return attr?.Name ?? propertyInfo.Name;
    }

    #endregion

    #region SelectColumn Extensions

    /// <summary>
    /// Adds a column to the query using a property expression.
    /// </summary>
    public static void Add<T>(
        this SelectColumnCollection columns,
        Expression<Func<T, object?>> expression,
        FromTerm table)
    {
        var columnName = ColumnName(expression);
        columns.Add(new SelectColumn(columnName, table));
    }

    /// <summary>
    /// Adds a column to the query with an alias.
    /// </summary>
    public static void Add<T>(
        this SelectColumnCollection columns,
        Expression<Func<T, object?>> expression,
        FromTerm table,
        string alias)
    {
        var columnName = ColumnName(expression);
        columns.Add(new SelectColumn(columnName, table, alias));
    }

    /// <summary>
    /// Adds all non-ignored properties from a type as columns.
    /// </summary>
    public static void AddAllColumns<T>(
        this SelectColumnCollection columns,
        FromTerm table)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetCustomAttribute<IgnoreColumnAttribute>() == null)
            .GroupBy(p => p.Name)
            .Select(g => g.First());

        foreach (var prop in properties)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnNameAttribute>();
            var columnName = columnAttr?.Name ?? prop.Name;
            columns.Add(new SelectColumn(columnName, table));
        }
    }

    #endregion

    #region SqlExpression Extensions

    /// <summary>
    /// Creates a field expression using a property expression.
    /// </summary>
    public static SqlExpression Field<T>(
        Expression<Func<T, object?>> expression,
        FromTerm table)
    {
        var columnName = ColumnName(expression);
        return SqlExpression.Field(columnName, table);
    }

    #endregion

    #region Query Generation

    /// <summary>
    /// Generates a SelectQuery from an object type, including all non-ignored properties.
    /// </summary>
    public static SelectQuery GenerateSelectQuery<T>(string? alias = null)
    {
        var tableName = TableName<T>();
        var tableAlias = alias ?? TableAlias<T>();

        var query = new SelectQuery(FromTerm.Table(tableName, tableAlias));
        query.Columns.AddAllColumns<T>(query.FromClause.BaseTable);

        return query;
    }

    #endregion

    #region Private Helpers

    private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object?>> expression)
    {
        MemberExpression? memberExpr = expression.Body as MemberExpression;

        // Handle boxing (value types wrapped in Convert)
        if (memberExpr == null && expression.Body is UnaryExpression unaryExpr)
        {
            memberExpr = unaryExpr.Operand as MemberExpression;
        }

        if (memberExpr?.Member is PropertyInfo propertyInfo)
        {
            return propertyInfo;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(expression));
    }

    #endregion
}

