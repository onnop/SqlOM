using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM;

/// <summary>
/// Internal helper methods for SqlOM strongly-typed query building.
/// </summary>
internal static class SqlOMExtensions
{
    private static readonly AsyncLocal<Dictionary<string, int>?> _aliasContext = new();

    /// <summary>
    /// Resets alias tracking. Called automatically by query constructors.
    /// </summary>
    internal static void ResetAliases()
    {
        _aliasContext.Value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a unique alias, auto-incrementing on collision.
    /// </summary>
    internal static string GetUniqueAlias(string baseAlias)
    {
        var context = _aliasContext.Value;
        if (context == null)
        {
            _aliasContext.Value = context = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        if (!context.TryGetValue(baseAlias, out var count))
        {
            context[baseAlias] = 1;
            return baseAlias;
        }

        context[baseAlias] = count + 1;
        return $"{baseAlias}{count + 1}";
    }

    /// <summary>
    /// Gets the table name for a type, using [TableName] attribute or pluralized type name.
    /// </summary>
    internal static string TableName<T>(bool pluralize = true)
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<TableNameAttribute>();
        if (attr != null) return attr.Name;
        return pluralize ? Pluralize(type.Name) : type.Name;
    }

    /// <summary>
    /// Simple pluralization for common English nouns.
    /// </summary>
    internal static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;

        if (word.EndsWith('y') && word.Length > 1)
        {
            var beforeY = word[^2];
            if (!"aeiouAEIOU".Contains(beforeY))
                return word[..^1] + "ies";
        }

        if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
            word.EndsWith("ch", StringComparison.Ordinal) ||
            word.EndsWith("sh", StringComparison.Ordinal))
        {
            return word + "es";
        }

        return word + "s";
    }

    /// <summary>
    /// Gets the table alias for a type, using [TableAlias] attribute or first letter.
    /// </summary>
    internal static string TableAlias<T>()
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<TableAliasAttribute>();
        return attr?.Alias ?? type.Name[..1].ToLowerInvariant();
    }

    /// <summary>
    /// Gets the column name for a property, using [ColumnName] attribute or property name.
    /// </summary>
    internal static string ColumnName<T>(Expression<Func<T, object?>> expression)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var attr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        return attr?.Name ?? propertyInfo.Name;
    }

    internal static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object?>> expression)
    {
        MemberExpression? memberExpr = expression.Body as MemberExpression;
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
}
