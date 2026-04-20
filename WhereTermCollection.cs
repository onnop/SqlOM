using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="WhereTerm"/> objects.
/// </summary>
[Serializable]
public class WhereTermCollection : Collection<WhereTerm>
{
    /// <summary>
    /// Creates a new, empty <see cref="WhereTermCollection"/>.
    /// </summary>
    public WhereTermCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="WhereTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public WhereTermCollection(IEnumerable<WhereTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="WhereTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public WhereTermCollection(WhereTerm[] items) : this((IEnumerable<WhereTerm>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<WhereTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
