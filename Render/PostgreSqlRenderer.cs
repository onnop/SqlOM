using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render;

/// <summary>
/// Renderer for PostgreSQL
/// </summary>
/// <remarks>
/// Use PostgreSqlRenderer to render SQL statements for PostgreSQL.
/// PostgreSQL uses LIMIT/OFFSET for paging and does not support the T-SQL "TOP n" or "WITH CUBE/ROLLUP" clauses.
/// </remarks>
public class PostgreSqlRenderer : SqlOmRenderer
{
    /// <summary>
    /// Creates a new PostgreSqlRenderer
    /// </summary>
    public PostgreSqlRenderer() : base('"', '"')
    {
    }

    /// <inheritdoc />
    protected override void IfNull(StringBuilder builder, SqlExpression expr)
    {
        builder.Append("coalesce(");
        if (expr.SubExpr1 is not null)
            Expression(builder, expr.SubExpr1);
        builder.Append(", ");
        if (expr.SubExpr2 is not null)
            Expression(builder, expr.SubExpr2);
        builder.Append(')');
    }

    /// <summary>
    /// Renders a SELECT statement for PostgreSQL, using LIMIT/OFFSET for paging and limits.
    /// </summary>
    /// <param name="query">Query definition</param>
    /// <returns>Generated SQL statement</returns>
    public override string RenderSelect(SelectQuery query)
    {
        query.Validate();

        StringBuilder selectBuilder = new();

        if (query.CommonTableExpressions.Count > 0)
        {
            RenderCommonTableExpressions(selectBuilder, query.CommonTableExpressions);
        }

        Select(selectBuilder, query.Distinct);

        SelectColumns(selectBuilder, query.Columns);

        FromClause(selectBuilder, query.FromClause, query.TableSpace);

        Where(selectBuilder, query.WherePhrase);
        WhereClause(selectBuilder, query.WherePhrase);

        GroupBy(selectBuilder, query.GroupByTerms);
        GroupByTerms(selectBuilder, query.GroupByTerms);

        if (query.GroupByWithCube)
        {
            // PostgreSQL uses GROUP BY CUBE(...) syntax inside the group-by list; the "WITH CUBE" T-SQL modifier is not supported.
            throw new InvalidQueryException("PostgreSQL does not support the WITH CUBE modifier. Use GROUP BY CUBE(col1, col2) via raw grouping instead.");
        }

        if (query.GroupByWithRollup)
        {
            throw new InvalidQueryException("PostgreSQL does not support the WITH ROLLUP modifier. Use GROUP BY ROLLUP(col1, col2) via raw grouping instead.");
        }

        Having(selectBuilder, query.HavingPhrase);
        WhereClause(selectBuilder, query.HavingPhrase);

        OrderBy(selectBuilder, query.OrderByTerms);
        OrderByTerms(selectBuilder, query.OrderByTerms);

        bool hasPaging = query.PageIndex >= 0 && query.PageSize > 0;
        if (hasPaging)
        {
            int offset = query.PageIndex * query.PageSize;
            selectBuilder.Append(" limit ");
            selectBuilder.Append(query.PageSize.ToString(CultureInfo.InvariantCulture));
            selectBuilder.Append(" offset ");
            selectBuilder.Append(offset.ToString(CultureInfo.InvariantCulture));
        }
        else if (query.Top > -1)
        {
            selectBuilder.Append(" limit ");
            selectBuilder.Append(query.Top.ToString(CultureInfo.InvariantCulture));
        }

        return selectBuilder.ToString();
    }

    /// <summary>
    /// Renders a row count SELECT statement.
    /// </summary>
    /// <param name="query">Query definition to count rows for</param>
    /// <returns>Generated SQL statement</returns>
    public override string RenderRowCount(SelectQuery query)
    {
        string baseSql = RenderSelect(query);

        SelectQuery countQuery = new SelectQuery();
        SelectColumn col = new SelectColumn("*", (FromTerm?)null, "cnt", SqlAggregationFunction.Count);
        countQuery.Columns.Add(col);
        countQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "t");
        return RenderSelect(countQuery);
    }

    /// <summary>
    /// Renders a paged SELECT statement using PostgreSQL LIMIT/OFFSET.
    /// </summary>
    /// <param name="pageIndex">The zero based index of the page to be returned</param>
    /// <param name="pageSize">The size of a page</param>
    /// <param name="totalRowCount">Ignored - PostgreSQL does not need a total row count for LIMIT/OFFSET paging.</param>
    /// <param name="query">Query definition to apply paging on</param>
    public override string RenderPage(int pageIndex, int pageSize, int totalRowCount, SelectQuery query)
    {
        SelectQuery pagedQuery = query.Clone();
        pagedQuery.SetPaging(pageIndex, pageSize);
        return RenderSelect(pagedQuery);
    }
}
