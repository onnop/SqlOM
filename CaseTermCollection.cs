using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="CaseTerm"/> objects.
/// </summary>
[Serializable]
public class CaseTermCollection : Collection<CaseTerm>
{
    /// <summary>
    /// Creates a new, empty <see cref="CaseTermCollection"/>.
    /// </summary>
    public CaseTermCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="CaseTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public CaseTermCollection(IEnumerable<CaseTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="CaseTermCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public CaseTermCollection(CaseTerm[] items) : this((IEnumerable<CaseTerm>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<CaseTerm> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }
}
