using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="SelectColumn"/> objects.
/// </summary>
[Serializable]
public class SelectColumnCollection : Collection<SelectColumn>
{
    /// <summary>
    /// Creates a new, empty <see cref="SelectColumnCollection"/>.
    /// </summary>
    public SelectColumnCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="SelectColumnCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public SelectColumnCollection(IEnumerable<SelectColumn> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="SelectColumnCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public SelectColumnCollection(SelectColumn[] items) : this((IEnumerable<SelectColumn>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<SelectColumn> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Adds a column derived from a strongly-typed property expression.
    /// Auto-aliases when [ColumnName] differs from the property name (for Dapper mapping).
    /// </summary>
    public void Add<T>(Expression<Func<T, object?>> expression, FromTerm table)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var columnAttr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        var columnName = columnAttr?.Name ?? propertyInfo.Name;

        if (columnAttr != null && columnAttr.Name != propertyInfo.Name)
            Add(new SelectColumn(columnName, table, propertyInfo.Name));
        else
            Add(new SelectColumn(columnName, table));
    }

    /// <summary>
    /// Adds a column derived from a strongly-typed property expression with an explicit alias.
    /// </summary>
    public void Add<T>(Expression<Func<T, object?>> expression, FromTerm table, string alias)
    {
        var propertyInfo = GetPropertyInfo(expression);
        var columnAttr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
        var columnName = columnAttr?.Name ?? propertyInfo.Name;
        Add(new SelectColumn(columnName, table, alias));
    }

    /// <summary>
    /// Adds all non-ignored properties of <typeparamref name="T"/> as columns.
    /// Skips properties marked with [IgnoreColumn] / [NotMapped] and any navigation properties.
    /// </summary>
    public void AddAllColumns<T>(FromTerm table)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !ShouldIgnoreProperty(p))
            .GroupBy(p => p.Name)
            .Select(g => g.First());

        foreach (var prop in properties)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnNameAttribute>();
            var columnName = columnAttr?.Name ?? prop.Name;
            Add(new SelectColumn(columnName, table));
        }
    }

    private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object?>> expression)
    {
        MemberExpression? member = expression.Body as MemberExpression;
        if (member == null && expression.Body is UnaryExpression unary)
            member = unary.Operand as MemberExpression;

        return member?.Member as PropertyInfo
               ?? throw new ArgumentException("Expression must be a property access");
    }

    private static bool ShouldIgnoreProperty(PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<IgnoreColumnAttribute>() != null) return true;
        if (prop.CustomAttributes.Any(a => a.AttributeType.Name == "NotMappedAttribute")) return true;
        return IsNavigationProperty(prop.PropertyType);
    }

    private static bool IsNavigationProperty(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null) type = underlyingType;
        if (type.IsPrimitive || type.IsEnum || type.IsValueType) return false;
        if (type == typeof(string) || type == typeof(byte[])) return false;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal)) return false;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string)) return true;
        return type.IsClass;
    }
}
