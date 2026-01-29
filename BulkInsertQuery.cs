using System.Diagnostics;

namespace Reeb.SqlOM
{
    /// <summary>
    /// Encapsulates a SQL BULK INSERT statement
    /// </summary>
    /// <remarks>
    /// Use InsertQuery to insert multiple new rows into a database table at once.
    /// Set <see cref="TableName"/> to the table you would like to insert into and use
    /// the <see cref="Terms"/> collection to specify values to be inserted.
    /// </remarks>
    /// <example>
    /// <code>
    /// BulkInsertQuery bulkInsert = new BulkInsertQuery("products");
    /// 
    /// InsertQuery query1 = new InsertQuery("products");
    /// query1.Terms.Add(new UpdateTerm("productId", SqlExpression.Number(999)));
    /// query1.Terms.Add(new UpdateTerm("name", SqlExpression.String("Temporary Test Product")));
    /// query1.Terms.Add(new UpdateTerm("price", SqlExpression.Number(123.45)));
    /// bulkInsert.Terms.Add(query1);
    ///  
    /// InsertQuery query2 = new InsertQuery("products");
    /// query2.Terms.Add(new UpdateTerm("productId", SqlExpression.Number(998)));
    /// query2.Terms.Add(new UpdateTerm("name", SqlExpression.String("Temporary Test Product")));
    /// query2.Terms.Add(new UpdateTerm("price", SqlExpression.Number(123.45)));
    /// bulkInsert.Terms.Add(query2);
    /// 
    /// RenderInsert(bulkInsert);
    /// </code>
    /// </example>
    [DebuggerDisplay("SQL = {SQL}")]
    public class BulkInsertQuery
    {
        InsertQueryCollection terms = new InsertQueryCollection();
        string tableName;

        /// <summary>
        /// Used for debugger purposes only
        /// Do not use to get the actual statement
        /// This always renders to a SQL Server string.
        /// </summary>
        private string SQL { get { return new Render.SqlServerRenderer().RenderBulkInsert(this); } }

        /// <summary>
        /// Create a BulkInsertQuery and resets alias tracking for Table&lt;T&gt;().
        /// </summary>
        public BulkInsertQuery() : this(null)
        {
        }

        /// <summary>
        /// Create a BulkInsertQuery and resets alias tracking for Table&lt;T&gt;().
        /// </summary>
        /// <param name="tableName">The name of the table to be inserted into</param>
        public BulkInsertQuery(string tableName)
        {
            SqlOMExtensions.ResetAliases();
            this.tableName = tableName;
        }

        /// <summary>
        /// Gets the collection if column-value pairs
        /// </summary>
        /// <remarks>
        /// Terms specify which values should be inserted into the table.
        /// </remarks>
        public InsertQueryCollection Terms
        {
            get { return this.terms; }
        }

        /// <summary>
        /// Gets or set the name of a table to be inserted into
        /// </summary>
        public string TableName
        {
            get { return this.tableName; }
            set { this.tableName = value; }
        }

        /// <summary>
        /// Validates BulkInsertQuery
        /// </summary>
        public void Validate()
        {
            if (tableName == null)
                throw new InvalidQueryException("TableName is empty.");
            if (terms.Count == 0)
                throw new InvalidQueryException("Terms collection is empty.");

            if (terms.Cast<InsertQuery>().Any(query => TableName != query.TableName))
                throw new InvalidQueryException("TableName should be the same in the sub queries.");

            terms.Validate();
        }
    }
}
