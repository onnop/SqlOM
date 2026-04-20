using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="UpdateTerm"/> objects.
/// </summary>
[Serializable]
public class UpdateTermCollection : Collection<UpdateTerm>
{
    /// <summary>
    /// Creates a new, empty <see cref="UpdateTermCollection"/>.
    /// </summary>
    public UpdateTermCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="UpdateTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public UpdateTermCollection(IEnumerable<UpdateTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="UpdateTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public UpdateTermCollection(UpdateTerm[] items) : this((IEnumerable<UpdateTerm>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<UpdateTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
