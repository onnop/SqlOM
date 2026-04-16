using System.Text;

namespace Reeb.SqlOM.Render
{
    /// <summary>
    /// Renderer for PostgreSQL
    /// </summary>
    /// <remarks>
    /// Use PostgreSqlRenderer to render SQL statements for PostgreSQL database.
    /// </remarks>
    public class PostgreSqlRenderer : SqlOmRenderer
    {
        /// <summary>
        /// Creates a new PostgreSqlRenderer
        /// </summary>
        public PostgreSqlRenderer() : base('"', '"')
        {
        }

        /// <summary>
        /// Renders IfNull SqlExpression
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected override void IfNull(StringBuilder builder, SqlExpression expr)
        {
            builder.Append("coalesce(");
            Expression(builder, expr.SubExpr1);
            builder.Append(", ");
            Expression(builder, expr.SubExpr2);
            builder.Append(")");
        }

        /// <summary>
        /// Renders a SELECT statement
        /// </summary>
        /// <param name="query">Query definition</param>
        /// <returns>Generated SQL statement</returns>
        public override string RenderSelect(SelectQuery query)
        {
            if (query.PageIndex >= 0 && query.PageSize > 0)
            {
                return RenderSelectWithPaging(query);
            }
            return RenderSelect(query, false);
        }

        private string RenderSelectWithPaging(SelectQuery query)
        {
            SelectQuery workingQuery = query.Clone();
            workingQuery.SetPaging(0, 0);

            StringBuilder selectBuilder = new();

            // Render CTEs if any
            if (workingQuery.CommonTableExpressions.Count > 0)
            {
                RenderCommonTableExpressions(selectBuilder, workingQuery.CommonTableExpressions);
            }

            // Start the select statement
            Select(selectBuilder, workingQuery.Distinct);

            // Render Top clause (PostgreSQL uses LIMIT)
            if (workingQuery.Top > -1)
                selectBuilder.AppendFormat("top {0} ", workingQuery.Top);

            // Render select columns
            SelectColumns(selectBuilder, workingQuery.Columns);

            FromClause(selectBuilder, workingQuery.FromClause, workingQuery.TableSpace);

            Where(selectBuilder, workingQuery.WherePhrase);
            WhereClause(selectBuilder, workingQuery.WherePhrase);

            GroupBy(selectBuilder, workingQuery.GroupByTerms);
            GroupByTerms(selectBuilder, workingQuery.GroupByTerms);

            if (workingQuery.GroupByWithCube)
                selectBuilder.Append(" with cube");
            else if (workingQuery.GroupByWithRollup)
                selectBuilder.Append(" with rollup");

            Having(selectBuilder, workingQuery.HavingPhrase);
            WhereClause(selectBuilder, workingQuery.HavingPhrase);

            OrderBy(selectBuilder, workingQuery.OrderByTerms);
            OrderByTerms(selectBuilder, workingQuery.OrderByTerms);

            // Add LIMIT and OFFSET
            int offset = query.PageIndex * query.PageSize;
            selectBuilder.AppendFormat(" LIMIT {0} OFFSET {1}", query.PageSize, offset);

            return selectBuilder.ToString();
        }

        string RenderSelect(SelectQuery query, bool forRowCount)
        {
            query.Validate();

            StringBuilder selectBuilder = new();

            // Render CTEs if any
            if (query.CommonTableExpressions.Count > 0)
            {
                RenderCommonTableExpressions(selectBuilder, query.CommonTableExpressions);
            }

            // Start the select statement
            Select(selectBuilder, query.Distinct);

            // Render Top clause
            if (query.Top > -1)
                selectBuilder.AppendFormat("top {0} ", query.Top);

            // Render select columns
            SelectColumns(selectBuilder, query.Columns);

            FromClause(selectBuilder, query.FromClause, query.TableSpace);

            Where(selectBuilder, query.WherePhrase);
            WhereClause(selectBuilder, query.WherePhrase);

            GroupBy(selectBuilder, query.GroupByTerms);
            GroupByTerms(selectBuilder, query.GroupByTerms);

            if (query.GroupByWithCube)
                selectBuilder.Append(" with cube");
            else if (query.GroupByWithRollup)
                selectBuilder.Append(" with rollup");

            Having(selectBuilder, query.HavingPhrase);
            WhereClause(selectBuilder, query.HavingPhrase);

            OrderBy(selectBuilder, query.OrderByTerms);
            OrderByTerms(selectBuilder, query.OrderByTerms);

            return selectBuilder.ToString();
        }

        /// <summary>
        /// Renders a row count SELECT statement.
        /// </summary>
        /// <param name="query">Query definition to count rows for</param>
        /// <returns>Generated SQL statement</returns>
        public override string RenderRowCount(SelectQuery query)
        {
            string baseSql = RenderSelect(query, false);

            SelectQuery countQuery = new SelectQuery();
            SelectColumn col = new SelectColumn("*", null, "cnt", SqlAggregationFunction.Count);
            countQuery.Columns.Add(col);
            countQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "t");
            return RenderSelect(countQuery, false);
        }

        /// <summary>
        /// Renders a paged SELECT statement
        /// </summary>
        /// <param name="pageIndex">The zero based index of the page to be returned</param>
        /// <param name="pageSize">The size of a page</param>
        /// <param name="totalRowCount">Total number of rows the query would yield if not paged</param>
        /// <param name="query">Query definition to apply paging on</param>
        /// <returns>Generated SQL statement</returns>
        public override string RenderPage(int pageIndex, int pageSize, int totalRowCount, SelectQuery query)
        {
            // PostgreSQL uses LIMIT/OFFSET, so we can use the built-in paging
            SelectQuery pagedQuery = query.Clone();
            pagedQuery.SetPaging(pageIndex, pageSize);
            return RenderSelect(pagedQuery);
        }
    }
}