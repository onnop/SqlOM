using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="InsertQuery"/> objects. Used by
/// <see cref="BulkInsertQuery"/> to represent multiple INSERT rows that must share schema.
/// </summary>
[Serializable]
public class InsertQueryCollection : Collection<InsertQuery>
{
    /// <summary>
    /// Creates a new, empty <see cref="InsertQueryCollection"/>.
    /// </summary>
    public InsertQueryCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="InsertQueryCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public InsertQueryCollection(IEnumerable<InsertQuery> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="InsertQueryCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public InsertQueryCollection(InsertQuery[] items) : this((IEnumerable<InsertQuery>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<InsertQuery> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Returns the total number of update terms across all queries in this collection.
    /// </summary>
    public int TotalTermCount()
    {
        return this.Sum(query => query.Terms.Count);
    }

    /// <summary>
    /// Validates that every query inserts the same set of columns and that all values are constants.
    /// </summary>
    public void Validate()
    {
        if (!this.All(query => query.Terms.All(update => update.Value.Type == SqlExpressionType.Constant
                                                          || update.Value.Type == SqlExpressionType.Null)))
            throw new InvalidQueryException("Invalid SqlExpressionType.");

        if (Count == 0) return;

        InsertQuery first = this[0];
        var firstTerms = first.Terms;
        if (this.Any(query => !query.Terms.SequenceEqual(firstTerms, new FieldComparer())))
            throw new InvalidQueryException("Each insert query in a bulk insert query must have the same fields.");
    }

    private sealed class FieldComparer : IEqualityComparer<UpdateTerm>
    {
        public bool Equals(UpdateTerm? x, UpdateTerm? y)
        {
            return x != null && y != null && string.Equals(x.FieldName, y.FieldName, StringComparison.Ordinal);
        }

        public int GetHashCode(UpdateTerm obj)
        {
            return obj is null ? 0 : obj.FieldName.GetHashCode(StringComparison.Ordinal);
        }
    }
}
