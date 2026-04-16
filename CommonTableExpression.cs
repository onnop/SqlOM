namespace Reeb.SqlOM
{
    /// <summary>
    /// Represents a Common Table Expression (CTE) in a SQL query
    /// </summary>
    public class CommonTableExpression
    {
        private string name;
        private SelectQuery query;
        private string[]? columnNames;

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
    }
}