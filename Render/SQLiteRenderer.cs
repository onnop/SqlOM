using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render;

/// <summary>
/// Renderer for SQLite
/// </summary>
/// <remarks>
/// Use SQLiteRenderer to render SQL statements for SQLite.
/// SQLite uses LIMIT for row limiting and does not support WITH CUBE / WITH ROLLUP.
/// </remarks>
public class SQLiteRenderer : SqlOmRenderer
{
    /// <summary>
    /// Creates a new SQLiteRenderer
    /// </summary>
    public SQLiteRenderer() : base('[', ']')
    {
        DateFormat = "yyyy-MM-dd";
        DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }

    /// <inheritdoc />
    protected override void Constant(StringBuilder builder, SqlConstant expr)
    {
        SqlDataType type = expr.Type;

        if (type == SqlDataType.Boolean)
            builder.Append((bool)expr.Value ? "1" : "0");
        else if (type == SqlDataType.Number)
            builder.AppendFormat(LiteralCulture, "{0}", expr.Value);
        else if (type == SqlDataType.Guid)
        {
            builder.Append('\'');
            builder.Append(SqlEncode(expr.Value.ToString() ?? string.Empty));
            builder.Append('\'');
        }
        else if (type == SqlDataType.Binary)
            builder.Append(ByteArrayToHexString((byte[])expr.Value));
        else if (type == SqlDataType.String)
        {
            if (expr.Value is null)
                builder.Append("null");
            else
            {
                builder.Append('\'');
                builder.Append(SqlEncode(expr.Value.ToString() ?? string.Empty));
                builder.Append('\'');
            }
        }
        else if (type == SqlDataType.Date)
        {
            DateTime val = (DateTime)expr.Value;
            bool dateOnly = val.Hour == 0 && val.Minute == 0 && val.Second == 0 && val.Millisecond == 0;
            string format = dateOnly ? dateFormat : dateTimeFormat;
            builder.Append('\'');
            builder.Append(val.ToString(format, LiteralCulture));
            builder.Append('\'');
        }
    }

    /// <inheritdoc />
    protected override void IfNull(StringBuilder builder, SqlExpression expr)
    {
        builder.Append("ifnull(");
        if (expr.SubExpr1 is not null)
            Expression(builder, expr.SubExpr1);
        builder.Append(", ");
        if (expr.SubExpr2 is not null)
            Expression(builder, expr.SubExpr2);
        builder.Append(')');
    }

    /// <summary>
    /// Renders a SELECT statement
    /// </summary>
    /// <param name="query">Query definition</param>
    /// <returns>Generated SQL statement</returns>
    public override string RenderSelect(SelectQuery query)
    {
        return RenderSelect(query, true);
    }

    string RenderSelect(SelectQuery query, bool renderOrderBy)
    {
        query.Validate();

        StringBuilder selectBuilder = new();

        if (query.CommonTableExpressions.Count > 0)
            RenderCommonTableExpressions(selectBuilder, query.CommonTableExpressions);

        Select(selectBuilder, query.Distinct);

        SelectColumns(selectBuilder, query.Columns);

        FromClause(selectBuilder, query.FromClause, query.TableSpace);

        Where(selectBuilder, query.WherePhrase);
        WhereClause(selectBuilder, query.WherePhrase);

        GroupBy(selectBuilder, query.GroupByTerms);
        GroupByTerms(selectBuilder, query.GroupByTerms);

        if (query.GroupByWithCube)
            throw new InvalidQueryException("SQLite does not support the WITH CUBE modifier.");
        if (query.GroupByWithRollup)
            throw new InvalidQueryException("SQLite does not support the WITH ROLLUP modifier.");

        Having(selectBuilder, query.HavingPhrase);
        WhereClause(selectBuilder, query.HavingPhrase);

        if (renderOrderBy)
        {
            OrderBy(selectBuilder, query.OrderByTerms);
            OrderByTerms(selectBuilder, query.OrderByTerms);
        }

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
    public override string RenderRowCount(SelectQuery query)
    {
        string baseSql = RenderSelect(query, false);

        SelectQuery countQuery = new();
        SelectColumn col = new("*", null!, "cnt", SqlAggregationFunction.Count);
        countQuery.Columns.Add(col);
        countQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "t");
        return RenderSelect(countQuery);
    }

    /// <summary>
    /// Renders a paged SELECT statement using SQLite LIMIT/OFFSET.
    /// </summary>
    public override string RenderPage(int pageIndex, int pageSize, int totalRowCount, SelectQuery query)
    {
        SelectQuery pagedQuery = query.Clone();
        pagedQuery.SetPaging(pageIndex, pageSize);
        return RenderSelect(pagedQuery);
    }
}
