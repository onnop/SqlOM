using System.Collections;

namespace Reeb.SqlOM
{
    /// <summary>
    /// A collection of Common Table Expressions
    /// </summary>
    public class CommonTableExpressionCollection : CollectionBase
    {
        /// <summary>
        /// Adds a CTE to the collection
        /// </summary>
        /// <param name="cte">The CTE to add</param>
        public void Add(CommonTableExpression cte)
        {
            List.Add(cte);
        }

        /// <summary>
        /// Gets a CTE at the specified index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The CTE at the index</returns>
        public CommonTableExpression this[int index]
        {
            get { return (CommonTableExpression)List[index]; }
        }

        /// <summary>
        /// Clones the CTE collection
        /// </summary>
        /// <returns>A new collection with cloned CTEs</returns>
        public CommonTableExpressionCollection Clone()
        {
            CommonTableExpressionCollection newCollection = new();
            foreach (CommonTableExpression cte in this)
            {
                newCollection.Add(new CommonTableExpression(cte.Name, cte.Query.Clone(), cte.ColumnNames));
            }
            return newCollection;
        }
    }
}