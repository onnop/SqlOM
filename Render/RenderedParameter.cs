namespace Reeb.SqlOM.Render;

/// <summary>
/// A single parameter captured by <see cref="SqlOmRenderer"/> while rendering a parameterised command.
/// </summary>
public sealed class RenderedParameter
{
    /// <summary>
    /// The placeholder name as it appears in <see cref="RenderedCommand.Sql"/>
    /// (including the dialect prefix, e.g. <c>"@p0"</c>, <c>":p0"</c>, or <c>"$1"</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The CLR value to bind for this parameter. May be <see langword="null"/> for SQL <c>NULL</c>.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// The SqlOM data type the value was rendered as.
    /// </summary>
    public SqlDataType Type { get; }

    /// <summary>
    /// Creates a new <see cref="RenderedParameter"/>.
    /// </summary>
    public RenderedParameter(string name, object? value, SqlDataType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        Type = type;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name} = {Value ?? "NULL"} ({Type})";
}
