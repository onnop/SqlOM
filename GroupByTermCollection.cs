using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="GroupByTerm"/> objects.
/// </summary>
[Serializable]
public class GroupByTermCollection : Collection<GroupByTerm>
{
    /// <summary>
    /// Creates a new, empty <see cref="GroupByTermCollection"/>.
    /// </summary>
    public GroupByTermCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="GroupByTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public GroupByTermCollection(IEnumerable<GroupByTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="GroupByTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public GroupByTermCollection(GroupByTerm[] items) : this((IEnumerable<GroupByTerm>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<GroupByTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
