using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="CommonTableExpression"/> objects.
/// </summary>
[Serializable]
public class CommonTableExpressionCollection : Collection<CommonTableExpression>
{
    /// <summary>
    /// Creates a new, empty <see cref="CommonTableExpressionCollection"/>.
    /// </summary>
    public CommonTableExpressionCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="CommonTableExpressionCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public CommonTableExpressionCollection(IEnumerable<CommonTableExpression> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="CommonTableExpressionCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public CommonTableExpressionCollection(CommonTableExpression[] items)
        : this((IEnumerable<CommonTableExpression>)items)
    {
    }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<CommonTableExpression> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Returns a deep copy of this collection. Each contained CTE is cloned (including
    /// its <see cref="CommonTableExpression.IsRecursive"/> flag and any column-name list).
    /// </summary>
    public CommonTableExpressionCollection Clone()
    {
        var copy = new CommonTableExpressionCollection();
        foreach (var cte in this)
        {
            CommonTableExpression cloned = cte.ColumnNames is { Length: > 0 }
                ? new CommonTableExpression(cte.Name, cte.Query.Clone(), cte.ColumnNames)
                : new CommonTableExpression(cte.Name, cte.Query.Clone());
            cloned.IsRecursive = cte.IsRecursive;
            copy.Add(cloned);
        }
        return copy;
    }
}
