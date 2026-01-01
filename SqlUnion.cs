using System.Collections;
using System.Diagnostics;

namespace Reeb.SqlOM
{
    /// <summary>
    /// Encapsulates SQL DISTINCT or ALL modifiers
    /// </summary>
    public enum DistinctModifier
    {
        /// <summary>Only distinct rows will be returned</summary>
        Distinct,
        /// <summary>All rows will be returned</summary>
        All
    }

    internal class SqlUnionItem
    {
        public SelectQuery Query;
        public DistinctModifier RepeatingAction;

        public SqlUnionItem(SelectQuery query, DistinctModifier repeatingAction)
        {
            Query = query;
            RepeatingAction = repeatingAction;
        }
    }

    /// <summary>
    /// Encapsulates SQL UNION statement
    /// </summary>
    [DebuggerDisplay("SQL = {SQL}")]
    public class SqlUnion
    {
        ArrayList items = new ArrayList(5);

        /// <summary>
        /// Used for debugger purposes only
        /// Do not use to get the actual statement
        /// This always renders to a SQL Server string.
        /// </summary>
        private string SQL { get { return new Render.SqlServerRenderer().RenderUnion(this); } }

        /// <summary>
        /// Creates a new SqlUnion
        /// </summary>
        public SqlUnion()
        {
        }

        /// <summary>
        /// Adds a query to the UNION clause
        /// </summary>
        /// <param name="query">SelectQuery to be added</param>
        /// <remarks>Query will be added with DistinctModifier.Distinct </remarks>
        public void Add(SelectQuery query)
        {
            Add(query, DistinctModifier.Distinct);
        }

        /// <summary>
        /// Adds a query to the UNION clause with the specified DistinctModifier
        /// </summary>
        /// <param name="query">SelectQuery to be added</param>
        /// <param name="repeatingAction">Distinct modifier</param>
        public void Add(SelectQuery query, DistinctModifier repeatingAction)
        {
            items.Add(new SqlUnionItem(query, repeatingAction));
        }

        internal IList Items
        {
            get { return items; }
        }
    }
}
