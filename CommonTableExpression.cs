namespace Reeb.SqlOM;

/// <summary>
/// Represents a Common Table Expression (CTE) in a SQL query
/// </summary>
public class CommonTableExpression
{
    private readonly string name;
    private readonly SelectQuery query;
    private readonly string[]? columnNames;

    /// <summary>
    /// Creates a new CTE
    /// </summary>
    /// <param name="name">The name of the CTE</param>
    /// <param name="query">The query that defines the CTE</param>
    public CommonTableExpression(string name, SelectQuery query)
    {
        this.name = name;
        this.query = query;
    }

    /// <summary>
    /// Creates a new CTE with column names
    /// </summary>
    /// <param name="name">The name of the CTE</param>
    /// <param name="query">The query that defines the CTE</param>
    /// <param name="columnNames">The column names for the CTE</param>
    public CommonTableExpression(string name, SelectQuery query, string[] columnNames)
    {
        this.name = name;
        this.query = query;
        this.columnNames = columnNames;
    }

    /// <summary>
    /// Creates a new CTE
    /// </summary>
    /// <param name="name">The name of the CTE</param>
    /// <param name="query">The query that defines the CTE</param>
    /// <param name="isRecursive">When true, the CTE is a recursive CTE (emits WITH RECURSIVE)</param>
    public CommonTableExpression(string name, SelectQuery query, bool isRecursive)
        : this(name, query)
    {
        IsRecursive = isRecursive;
    }

    /// <summary>
    /// Gets the name of the CTE
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the query that defines the CTE
    /// </summary>
    public SelectQuery Query => query;

    /// <summary>
    /// Gets the column names for the CTE (optional)
    /// </summary>
    public string[]? ColumnNames => columnNames;

    /// <summary>
    /// Gets or sets whether this CTE is recursive. If any CTE in a query is recursive, the rendered query uses WITH RECURSIVE.
    /// </summary>
    /// <remarks>
    /// WITH RECURSIVE is the standard syntax supported by PostgreSQL, SQLite, MySQL 8+, MariaDB 10.2+, and Oracle 12c+.
    /// SQL Server supports recursive CTEs via plain WITH; the RECURSIVE keyword is not required but is tolerated by most tools.
    /// </remarks>
    public bool IsRecursive { get; set; }
}
