using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="OrderByTerm"/> objects.
/// </summary>
[Serializable]
public class OrderByTermCollection : Collection<OrderByTerm>
{
    /// <summary>
    /// Creates a new, empty <see cref="OrderByTermCollection"/>.
    /// </summary>
    public OrderByTermCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="OrderByTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public OrderByTermCollection(IEnumerable<OrderByTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="OrderByTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public OrderByTermCollection(OrderByTerm[] items) : this((IEnumerable<OrderByTerm>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<OrderByTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
