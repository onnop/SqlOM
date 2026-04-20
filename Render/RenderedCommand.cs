namespace Reeb.SqlOM.Render;

/// <summary>
/// Represents a rendered SQL command together with the parameter values that were
/// captured during rendering. Returned from the parameterised <c>Render*Command</c>
/// methods on <see cref="SqlOmRenderer"/> as a safer alternative to inline-literal SQL.
/// </summary>
/// <remarks>
/// The <see cref="Sql"/> string contains parameter placeholders (e.g. <c>@p0</c>, <c>:p0</c>,
/// or <c>$1</c>) that match the names of <see cref="Parameters"/>. Bind the parameters to your
/// <see cref="System.Data.IDbCommand"/> using whatever convention your provider expects
/// (Dapper, ADO.NET, Npgsql, etc.).
/// </remarks>
public sealed class RenderedCommand
{
    /// <summary>
    /// The rendered SQL with parameter placeholders substituted for constant literals.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// The parameters captured while rendering, in the order they were emitted.
    /// </summary>
    public IReadOnlyList<RenderedParameter> Parameters { get; }

    /// <summary>
    /// Creates a new <see cref="RenderedCommand"/>.
    /// </summary>
    public RenderedCommand(string sql, IReadOnlyList<RenderedParameter> parameters)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <inheritdoc />
    public override string ToString() => Sql;
}
