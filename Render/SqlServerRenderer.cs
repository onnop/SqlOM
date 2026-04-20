using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render;

/// <summary>
/// Renderer for Microsoft SQL Server
/// </summary>
/// <remarks>
/// Use SqlServerRenderer to render SQL statements for Microsoft SQL Server.
/// </remarks>
public class SqlServerRenderer : SqlOmRenderer
{
    /// <summary>
    /// Creates a new SqlServerRenderer
    /// </summary>
    public SqlServerRenderer() : base('[', ']')
    {
    }

    /// <summary>
    /// When true, paging is rendered using ORDER BY ... OFFSET n ROWS FETCH NEXT m ROWS ONLY
    /// (SQL Server 2012+). When false (default), paging uses a nested ROW_NUMBER() wrapper which
    /// works on SQL Server 2005+.
    /// </summary>
    public bool UseOffsetFetchPaging { get; set; }

    /// <summary>
    /// Renders a constant. SQL Server strings are emitted as Unicode (N'...') literals.
    /// </summary>
    protected override void Constant(StringBuilder builder, SqlConstant expr)
    {
        SqlDataType type = expr.Type;

        if (type == SqlDataType.Boolean)
            builder.Append((bool)expr.Value! ? "1" : "0");
        else if (type == SqlDataType.Number)
            builder.AppendFormat(LiteralCulture, "{0}", expr.Value);
        else if (type == SqlDataType.Guid)
        {
            builder.Append('\'');
            builder.Append(SqlEncode(expr.Value!.ToString() ?? string.Empty));
            builder.Append('\'');
        }
        else if (type == SqlDataType.Binary)
            builder.Append(ByteArrayToHexString((byte[]?)expr.Value));
        else if (type == SqlDataType.String)
        {
            if (expr.Value is null)
                builder.Append("null");
            else
            {
                builder.Append("N'");
                builder.Append(SqlEncode(expr.Value.ToString() ?? string.Empty));
                builder.Append('\'');
            }
        }
        else if (type == SqlDataType.Date)
        {
            DateTime val = (DateTime)expr.Value!;
            bool dateOnly = val.Hour == 0 && val.Minute == 0 && val.Second == 0 && val.Millisecond == 0;
            string format = dateOnly ? dateFormat : dateTimeFormat;
            builder.Append('\'');
            builder.Append(val.ToString(format, LiteralCulture));
            builder.Append('\'');
        }
    }

    /// <summary>
    /// Renders IfNull SqlExpression using SQL Server's isnull().
    /// </summary>
    protected override void IfNull(StringBuilder builder, SqlExpression expr)
    {
        builder.Append("isnull(");
        if (expr.SubExpr1 is not null)
            Expression(builder, expr.SubExpr1);
        builder.Append(", ");
        if (expr.SubExpr2 is not null)
            Expression(builder, expr.SubExpr2);
        builder.Append(')');
    }

    /// <summary>
    /// Emits SQL Server-specific table hints, including WITH (NOLOCK) when <see cref="SqlOmRenderer.ReadUncommitted"/> is set.
    /// </summary>
    protected override void ApplyTableHints(StringBuilder builder, FromTerm table)
    {
        if (ReadUncommitted && table.Type == FromTermType.Table)
        {
            builder.Append(" with (nolock)");
            return;
        }

        if (table.LockHint != LockHintType.NONE)
        {
            builder.Append(" WITH (");
            builder.Append(table.LockHint);
            builder.Append(')');
        }
    }

    /// <inheritdoc />
    protected override void DateDiff(StringBuilder builder, SqlExpression expression)
    {
        DateDiff datePartEnum = (DateDiff)expression.Value!;
        string datePartValue = datePartEnum switch
        {
            SqlOM.DateDiff.Year => "year",
            SqlOM.DateDiff.Quarter => "quarter",
            SqlOM.DateDiff.Month => "month",
            SqlOM.DateDiff.DayOfYear => "dayofyear",
            SqlOM.DateDiff.Day => "day",
            SqlOM.DateDiff.Week => "week",
            SqlOM.DateDiff.Hour => "hour",
            SqlOM.DateDiff.Minute => "minute",
            SqlOM.DateDiff.Second => "second",
            SqlOM.DateDiff.MilliSecond => "millisecond",
            SqlOM.DateDiff.MicroSecond => "microsecond",
            SqlOM.DateDiff.NanoSecond => "nanosecond",
            _ => throw new ArgumentOutOfRangeException(nameof(expression), $"Unknown DateDiff value: {datePartEnum}")
        };

        builder.Append("DATEDIFF(");
        builder.Append(datePartValue);
        builder.Append(',');
        Expression(builder, expression.SubExpr1!);
        builder.Append(',');
        Expression(builder, expression.SubExpr2!);
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
        bool hasPaging = query.PageIndex >= 0 && query.PageSize > 0;
        // If there should be paging, create it first (legacy ROW_NUMBER wrapper, unless OFFSET/FETCH is enabled).
        SelectQuery workingQuery = (hasPaging && !UseOffsetFetchPaging) ? Paging(query) : query;
        workingQuery.Validate();

        StringBuilder selectBuilder = new();

        // Render CTEs if any
        if (workingQuery.CommonTableExpressions.Count > 0)
        {
            RenderCommonTableExpressions(selectBuilder, workingQuery.CommonTableExpressions);
        }

        //Start the select statement
        Select(selectBuilder, workingQuery.Distinct);

        //Render Top clause
        if (workingQuery.Top > -1)
        {
            selectBuilder.Append("top ");
            selectBuilder.Append(workingQuery.Top.ToString(CultureInfo.InvariantCulture));
            selectBuilder.Append(' ');
        }

        //Render select columns
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

        if (renderOrderBy)
        {
            OrderBy(selectBuilder, workingQuery.OrderByTerms);
            OrderByTerms(selectBuilder, workingQuery.OrderByTerms);
        }

        if (hasPaging && UseOffsetFetchPaging)
        {
            if (workingQuery.OrderByTerms.Count == 0)
                throw new InvalidQueryException("OrderBy must be specified for OFFSET/FETCH paging on SQL Server.");
            int offset = query.PageIndex * query.PageSize;
            selectBuilder.Append(" offset ");
            selectBuilder.Append(offset.ToString(CultureInfo.InvariantCulture));
            selectBuilder.Append(" rows fetch next ");
            selectBuilder.Append(query.PageSize.ToString(CultureInfo.InvariantCulture));
            selectBuilder.Append(" rows only");
        }

        return selectBuilder.ToString();
    }

    /// <summary>
    /// Wraps the query in a ROW_NUMBER() + WHERE filter so that it returns a single page.
    /// Used as a SQL Server 2005+ compatible fallback; see <see cref="UseOffsetFetchPaging"/>
    /// for the SQL Server 2012+ OFFSET/FETCH alternative.
    /// </summary>
    public SelectQuery Paging(SelectQuery query)
    {
        // First create a clone of the query without any paging
        SelectQuery clone = query.Clone();
        clone.SetPaging(0, 0);

        // Wrap the query with a paged query
        SelectQuery wrapper = new(FromTerm.SubQuery(clone, "wrapper"));

        // Add a rowcount column to the query using the same ordering as the query
        StringBuilder columns = new();
        for (int i = 0; i < clone.OrderByTerms.Count; i++)
        {
            if (i > 0)
                columns.Append(", ");
            var orderByTerm = clone.OrderByTerms[i];
            OrderByTerm(columns, new OrderByTerm(orderByTerm.Field, wrapper.FromClause.BaseTable!, orderByTerm.Direction));
        }

        wrapper.Columns.Add(new SelectColumn("*", (FromTerm?)null));
        wrapper.Columns.Add(new SelectColumn(SqlExpression.Raw($"ROW_NUMBER() OVER (ORDER BY {columns})"), "ROW_NUMBER"));
        SelectQuery wrapperRowFilter = new(FromTerm.SubQuery(wrapper, "wrapperRowFilter"));

        SelectColumn? rownumber = null;
        foreach (SelectColumn column in clone.Columns)
            if (string.Equals(column.ColumnAlias, "ROW_NUMBER", StringComparison.OrdinalIgnoreCase))
                rownumber = column;

        // Make sure that the column is removed from 
        if (rownumber is not null)
            clone.Columns.Remove(rownumber);

        // Move the ordering to the wrapper as ordering is not allowed on subqueries
        FromTerm wrapperTable = wrapperRowFilter.FromClause.BaseTable!;
        foreach (OrderByTerm orderByTerm in clone.OrderByTerms)
            wrapperRowFilter.OrderByTerms.Add(new OrderByTerm(orderByTerm.Field, wrapperTable, orderByTerm.Direction));
        clone.OrderByTerms.Clear();

        // Filter on the rowcount in the wrapper
        wrapperRowFilter.WherePhrase.Terms.Add(WhereTerm.CreateBetween(SqlExpression.Field("ROW_NUMBER"), SqlExpression.Number(query.PageIndex * query.PageSize + 1), SqlExpression.Number(query.PageIndex * query.PageSize + query.PageSize)));
        return wrapperRowFilter;
    }

    /// <summary>
    /// Renders a row count SELECT statement.
    /// </summary>
    /// <param name="query">Query definition to count rows for</param>
    /// <returns>Generated SQL statement</returns>
    /// <remarks>
    /// Renders a SQL statement which returns a result set with one row and one cell which contains the number of rows <paramref name="query"/> can generate.
    /// The generated statement will work nicely with <see cref="System.Data.IDbCommand.ExecuteScalar"/> method.
    /// </remarks>
    public override string RenderRowCount(SelectQuery query)
    {
        string baseSql = RenderSelect(query, false);

        SelectQuery countQuery = new SelectQuery();
        SelectColumn col = new SelectColumn("*", (FromTerm?)null, "cnt", SqlAggregationFunction.Count);
        countQuery.Columns.Add(col);
        countQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "t");
        return RenderSelect(countQuery);
    }
}
