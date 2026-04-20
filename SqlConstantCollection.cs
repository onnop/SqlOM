using System.Collections;
using System.Collections.ObjectModel;

namespace Reeb.SqlOM;

/// <summary>
/// A strongly-typed collection of <see cref="SqlConstant"/> objects.
/// </summary>
[Serializable]
public class SqlConstantCollection : Collection<SqlConstant>
{
    /// <summary>
    /// Creates a new, empty <see cref="SqlConstantCollection"/>.
    /// </summary>
    public SqlConstantCollection()
    {
    }

    /// <summary>
    /// Creates a new <see cref="SqlConstantCollection"/> with the specified initial capacity.
    /// </summary>
    public SqlConstantCollection(int capacity) : base(new List<SqlConstant>(capacity))
    {
    }

    /// <summary>
    /// Creates a new <see cref="SqlConstantCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public SqlConstantCollection(IEnumerable<SqlConstant> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Creates a new <see cref="SqlConstantCollection"/> populated from <paramref name="items"/>.
    /// </summary>
    public SqlConstantCollection(SqlConstant[] items) : this((IEnumerable<SqlConstant>)items) { }

    /// <summary>
    /// Adds the elements of <paramref name="items"/> to the end of this collection.
    /// </summary>
    public void AddRange(IEnumerable<SqlConstant> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Adds a CLR value, automatically wrapping it in the appropriate <see cref="SqlConstant"/>
    /// based on its runtime type. Null values are ignored.
    /// </summary>
    public void Add(object? val)
    {
        if (val is null)
            return;

        SqlConstant constant = val switch
        {
            string s => SqlConstant.String(s),
            int i => SqlConstant.Number(i),
            long l => SqlConstant.Number(l),
            double d => SqlConstant.Number(d),
            float f => SqlConstant.Number(f),
            decimal m => SqlConstant.Number(m),
            DateTime dt => SqlConstant.Date(dt),
            Guid g => SqlConstant.Guid(g),
            SqlConstant sc => sc,
            _ => SqlConstant.String(val.ToString() ?? string.Empty)
        };

        Add(constant);
    }

    /// <summary>
    /// Builds a <see cref="SqlConstantCollection"/> from a sequence of GUIDs.
    /// </summary>
    public static SqlConstantCollection FromGuids(IEnumerable<Guid> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var collection = new SqlConstantCollection();
        foreach (var value in values)
            collection.Add(SqlConstant.Guid(value));
        return collection;
    }

    /// <summary>
    /// Builds a <see cref="SqlConstantCollection"/> from a list of arbitrary CLR values.
    /// Element types are inferred via <see cref="Add(object)"/>.
    /// </summary>
    public static SqlConstantCollection FromList(IList values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var collection = new SqlConstantCollection(values.Count);
        foreach (object val in values)
            collection.Add(val);
        return collection;
    }
}
