using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM;

/// <summary>
/// Generates unique table aliases, auto-incrementing on collision.
/// Example: "a", "s", "w", "a2" (if "a" already used)
/// </summary>
public class AliasGenerator
{
    private readonly Dictionary<string, int> _used = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a unique alias based on the type's first letter or [TableAlias] attribute.
    /// </summary>
    public string Next<T>() => GetUnique(SqlOMExtensions.TableAlias<T>());

    /// <summary>
    /// Gets a unique alias based on the provided base alias.
    /// </summary>
    public string Next(string baseAlias) => GetUnique(baseAlias);

    private string GetUnique(string baseAlias)
    {
        if (!_used.TryGetValue(baseAlias, out var count))
        {
            _used[baseAlias] = 1;
            return baseAlias;
        }

        _used[baseAlias] = count + 1;
        return $"{baseAlias}{count + 1}";
    }
}

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
    /// Gets the table name for a type, using [TableName] attribute or pluralized type name.
    /// </summary>
    /// <param name="pluralize">If true and no [TableName] attribute, pluralizes the type name (default: true).</param>
    public static string TableName<T>(bool pluralize = true)
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<TableNameAttribute>();
        if (attr != null)
            return attr.Name;
        
        return pluralize ? Pluralize(type.Name) : type.Name;
    }

    /// <summary>
    /// Simple pluralization for common English nouns.
    /// Handles: -y → -ies, -s/-x/-z/-ch/-sh → -es, default → -s
    /// </summary>
    public static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Words ending in consonant + y → ies
        if (word.EndsWith('y') && word.Length > 1)
        {
            var beforeY = word[^2];
            if (!"aeiouAEIOU".Contains(beforeY))
                return word[..^1] + "ies";
        }

        // Words ending in s, x, z, ch, sh → es
        if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
            word.EndsWith("ch", StringComparison.Ordinal) || 
            word.EndsWith("sh", StringComparison.Ordinal))
        {
            return word + "es";
        }

        // Default: add s
        return word + "s";
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
    /// Creates a FromTerm with auto-generated unique alias from the AliasGenerator.
    /// </summary>
    public static FromTerm Table<T>(AliasGenerator aliases)
    {
        return FromTerm.Table(TableName<T>(), aliases.Next<T>());
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
    /// Auto-aliases when [ColumnName] differs from property name (for Dapper mapping).
    /// </summary>
    public static void Add<T>(
        this SelectColumnCollection columns,
        Expression<Func<T, object?>> expression,
        FromTerm table)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var columnAttr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        var columnName = columnAttr?.Name ?? propertyInfo.Name;
        
        // Auto-alias when column name differs from property name
        if (columnAttr != null && columnAttr.Name != propertyInfo.Name)
        {
            columns.Add(new SelectColumn(columnName, table, propertyInfo.Name));
        }
        else
        {
            columns.Add(new SelectColumn(columnName, table));
        }
    }

    /// <summary>
    /// Adds a column to the query with an explicit alias (overrides auto-alias).
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
    /// Automatically ignores: [IgnoreColumn], [NotMapped], navigation properties (non-primitive reference types).
    /// </summary>
    public static void AddAllColumns<T>(
        this SelectColumnCollection columns,
        FromTerm table)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !ShouldIgnoreProperty(p))
            .GroupBy(p => p.Name)
            .Select(g => g.First());

        foreach (var prop in properties)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnNameAttribute>();
            var columnName = columnAttr?.Name ?? prop.Name;
            columns.Add(new SelectColumn(columnName, table));
        }
    }

    /// <summary>
    /// Determines if a property should be ignored when building queries.
    /// </summary>
    private static bool ShouldIgnoreProperty(PropertyInfo prop)
    {
        // Explicit [IgnoreColumn] attribute
        if (prop.GetCustomAttribute<IgnoreColumnAttribute>() != null)
            return true;

        // EF Core [NotMapped] attribute (check by name to avoid hard dependency)
        if (prop.CustomAttributes.Any(a => a.AttributeType.Name == "NotMappedAttribute"))
            return true;

        // Skip navigation properties (reference types except string, byte[], primitives)
        var propType = prop.PropertyType;
        if (IsNavigationProperty(propType))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a type represents a navigation property (should be ignored in column generation).
    /// </summary>
    private static bool IsNavigationProperty(Type type)
    {
        // Nullable<T> - check underlying type
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            type = underlyingType;

        // Primitives, enums, value types are columns
        if (type.IsPrimitive || type.IsEnum || type.IsValueType)
            return false;

        // String and byte[] are columns
        if (type == typeof(string) || type == typeof(byte[]))
            return false;

        // Common value-like types
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || 
            type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal))
            return false;

        // Collections are navigation properties
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
            return true;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return true;

        // Other reference types are likely navigation properties
        return type.IsClass;
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

