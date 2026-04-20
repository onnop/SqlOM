using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM;

/// <summary>
/// Helper methods for SqlOM strongly-typed query building.
/// </summary>
/// <remarks>
/// <para>
/// Alias tracking for <see cref="FromTerm.Table{T}()"/> is stored in an
/// <see cref="AsyncLocal{T}"/> slot so that auto-generated aliases (e.g. <c>c</c>, <c>c2</c>, <c>c3</c>)
/// remain unique within the async execution context that builds the query. The counter is reset
/// automatically at the top of every <see cref="SelectQuery"/>, <see cref="InsertQuery"/>,
/// <see cref="UpdateQuery"/>, <see cref="DeleteQuery"/>, and <see cref="BulkInsertQuery"/> constructor.
/// </para>
/// <para>
/// For nested or parallel query construction, call <see cref="BeginAliasScope"/> to get an
/// <see cref="IDisposable"/> that isolates the alias counters for the lifetime of the <c>using</c> block
/// and restores the prior counters on dispose.
/// </para>
/// </remarks>
public static class SqlOMExtensions
{
    private static readonly AsyncLocal<Dictionary<string, int>?> _aliasContext = new();
    private static readonly AsyncLocal<int> _aliasScopeDepth = new();

    /// <summary>
    /// Resets alias tracking. Called automatically by query constructors.
    /// No-op when a manual <see cref="BeginAliasScope"/> block is active, so sub-query
    /// construction inside an explicit scope cannot wipe the caller's alias counter.
    /// </summary>
    internal static void ResetAliases()
    {
        if (_aliasScopeDepth.Value > 0)
            return;

        _aliasContext.Value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Begins a nested alias-tracking scope. While the returned <see cref="IDisposable"/> is alive,
    /// calls to <see cref="FromTerm.Table{T}()"/> issue unique aliases within an isolated counter;
    /// the prior alias context is restored on disposal.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that restores the prior alias context when disposed.</returns>
    /// <example>
    /// <code>
    /// using (SqlOMExtensions.BeginAliasScope())
    /// {
    ///     var sub = new SelectQuery();
    ///     sub.FromClause.BaseTable = FromTerm.Table&lt;Product&gt;();
    ///     // 'p' is chosen inside this scope without affecting the outer counter
    /// }
    /// </code>
    /// </example>
    public static IDisposable BeginAliasScope()
    {
        var previous = _aliasContext.Value;
        _aliasContext.Value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _aliasScopeDepth.Value++;
        return new AliasScope(previous);
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

    private sealed class AliasScope : IDisposable
    {
        private readonly Dictionary<string, int>? _previous;
        private bool _disposed;

        internal AliasScope(Dictionary<string, int>? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _aliasContext.Value = _previous;
            _aliasScopeDepth.Value = Math.Max(0, _aliasScopeDepth.Value - 1);
        }
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

    #region Public Convenience Methods (for use with 'using static')

    /// <summary>
    /// Creates a FromTerm for a table using the type's [TableName] attribute or pluralized type name.
    /// Convenience method for use with 'using static Reeb.SqlOM.SqlOMExtensions;'
    /// </summary>
    public static FromTerm Table<T>() => FromTerm.Table<T>();

    /// <summary>
    /// Creates a FromTerm for a table with a specified alias.
    /// Convenience method for use with 'using static Reeb.SqlOM.SqlOMExtensions;'
    /// </summary>
    public static FromTerm Table<T>(string alias) => FromTerm.Table<T>(alias);

    /// <summary>
    /// Creates a SqlExpression for a field using a property expression.
    /// Convenience method for use with 'using static Reeb.SqlOM.SqlOMExtensions;'
    /// </summary>
    public static SqlExpression Field<T>(Expression<Func<T, object?>> expression, FromTerm table)
        => SqlExpression.Field(expression, table);

    /// <summary>
    /// Gets the column name for a property, using [ColumnName] attribute or property name.
    /// Public version for use with 'using static Reeb.SqlOM.SqlOMExtensions;'
    /// </summary>
    public static string ColumnName<T>(Expression<Func<T, object?>> expression)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var attr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        return attr?.Name ?? propertyInfo.Name;
    }

    /// <summary>
    /// Generates a complete SELECT query from a row type in one line.
    /// Creates a query with all non-ignored columns selected.
    /// Convenience method for use with 'using static Reeb.SqlOM.SqlOMExtensions;'
    /// </summary>
    public static SelectQuery GenerateSelectQuery<T>(string? alias = null)
    {
        return SelectQuery.For<T>(alias);
    }

    #endregion
}
