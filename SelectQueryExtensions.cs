using System.Linq.Expressions;

namespace Reeb.SqlOM
{
    /// <summary>
    /// Fluent API extensions for SelectQuery
    /// </summary>
    public static class SelectQueryExtensions
    {
        /// <summary>
        /// Adds a WHERE condition using a fluent API
        /// </summary>
        public static SelectQuery Where(this SelectQuery query, WhereTerm term)
        {
            query.WherePhrase.Terms.Add(term);
            return query;
        }

        /// <summary>
        /// Adds a WHERE condition for a field comparison
        /// </summary>
        public static SelectQuery Where<T>(this SelectQuery query, Expression<Func<T, object?>> fieldExpr, FromTerm table, CompareOperator op, SqlExpression value)
        {
            var field = SqlExpression.Field(fieldExpr, table);
            query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(field, value, op));
            return query;
        }

        /// <summary>
        /// Adds an ORDER BY clause
        /// </summary>
        public static SelectQuery OrderBy(this SelectQuery query, string field, FromTerm? table = null, OrderByDirection direction = OrderByDirection.Ascending)
        {
            query.OrderByTerms.Add(new OrderByTerm(field, table, direction));
            return query;
        }

        /// <summary>
        /// Adds an ORDER BY clause using an expression
        /// </summary>
        public static SelectQuery OrderBy<T>(this SelectQuery query, Expression<Func<T, object?>> fieldExpr, FromTerm table, OrderByDirection direction = OrderByDirection.Ascending)
        {
            var field = SqlExpression.Field(fieldExpr, table);
            query.OrderByTerms.Add(new OrderByTerm(field.ToString() ?? string.Empty, table, direction));
            return query;
        }

        /// <summary>
        /// Adds a GROUP BY clause
        /// </summary>
        public static SelectQuery GroupBy(this SelectQuery query, string field, FromTerm? table = null)
        {
            query.GroupByTerms.Add(new GroupByTerm(field, table));
            return query;
        }

        /// <summary>
        /// Adds a GROUP BY clause using an expression
        /// </summary>
        public static SelectQuery GroupBy<T>(this SelectQuery query, Expression<Func<T, object?>> fieldExpr, FromTerm table)
        {
            var field = SqlExpression.Field(fieldExpr, table);
            query.GroupByTerms.Add(new GroupByTerm(field.ToString() ?? string.Empty, table));
            return query;
        }

        /// <summary>
        /// Adds a column to the SELECT clause
        /// </summary>
        public static SelectQuery Select(this SelectQuery query, SelectColumn column)
        {
            query.Columns.Add(column);
            return query;
        }

        /// <summary>
        /// Adds a column using an expression
        /// </summary>
        public static SelectQuery Select<T>(this SelectQuery query, Expression<Func<T, object?>> fieldExpr, FromTerm table, string? alias = null)
        {
            if (alias != null)
                query.Columns.Add<T>(fieldExpr, table, alias);
            else
                query.Columns.Add<T>(fieldExpr, table);
            return query;
        }

        /// <summary>
        /// Adds a JOIN to the query
        /// </summary>
        public static SelectQuery Join(this SelectQuery query, JoinType joinType, FromTerm leftTable, FromTerm rightTable, string leftField, string rightField)
        {
            query.FromClause.Join(joinType, leftTable, rightTable, leftField, rightField);
            return query;
        }

        /// <summary>
        /// Adds a CTE to the query
        /// </summary>
        public static SelectQuery WithCte(this SelectQuery query, string name, SelectQuery cteQuery, string[]? columnNames = null)
        {
            query.CommonTableExpressions.Add(new CommonTableExpression(name, cteQuery, columnNames));
            return query;
        }

        /// <summary>
        /// Sets the TOP clause
        /// </summary>
        public static SelectQuery Top(this SelectQuery query, int top)
        {
            query.Top = top;
            return query;
        }

        /// <summary>
        /// Sets DISTINCT
        /// </summary>
        public static SelectQuery Distinct(this SelectQuery query)
        {
            query.Distinct = true;
            return query;
        }
    }
}