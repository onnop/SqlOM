using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="Join"/> objects.
/// </summary>
[Serializable]
internal class JoinCollection : Collection<Join>
{
    internal JoinCollection()
    {
    }

    internal JoinCollection(IEnumerable<Join> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    internal JoinCollection(Join[] items) : this((IEnumerable<Join>)items) { }

    internal void AddRange(IEnumerable<Join> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
