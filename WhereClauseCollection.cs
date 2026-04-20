using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="WhereClause"/> objects.
/// </summary>
[Serializable]
public class WhereClauseCollection : Collection<WhereClause>
{
    /// <summary>
    /// Creates a new, empty <see cref="WhereClauseCollection"/>.
    /// </summary>
    public WhereClauseCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="WhereClauseCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public WhereClauseCollection(IEnumerable<WhereClause> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="WhereClauseCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public WhereClauseCollection(WhereClause[] items) : this((IEnumerable<WhereClause>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<WhereClause> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
