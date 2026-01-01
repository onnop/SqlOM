using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render;

/// <summary>
/// Renderer for SqlServer
/// </summary>
/// <remarks>
/// Use SqlServerRenderer to render SQL statements for Microsoft SQL Server database.
/// This version of Sql.Net has been tested with MSSQL 2000
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
        /// Renders a constant
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected override void Constant(StringBuilder builder, SqlConstant expr)
        {
            SqlDataType type = expr.Type;

            if (type == SqlDataType.Boolean)
                builder.Append(((bool)expr.Value! ? "1" : "0"));
            if (type == SqlDataType.Number)
                builder.AppendFormat(new CultureInfo("en-US"), "{0}", expr.Value);
            else if (type == SqlDataType.Guid)
                builder.AppendFormat("'{0}'", expr.Value!.ToString());
            else if (type == SqlDataType.Binary)
                builder.Append(ByteArrayToHexString((byte[]?)expr.Value));
            else if (type == SqlDataType.String)
            {
                if (expr.Value is null)
                    builder.Append("null");
                else
                    builder.AppendFormat("N'{0}'", expr.Value);
            }
            else if (type == SqlDataType.Date)
            {
                DateTime val = (DateTime)expr.Value!;
                bool dateOnly = val.Hour == 0 && val.Minute == 0 && val.Second == 0 && val.Millisecond == 0;
                string format = dateOnly ? dateFormat : dateTimeFormat;
                builder.AppendFormat("'{0}'", val.ToString(format, new CultureInfo("en-us")));
            }
        }

        /// <summary>
        /// Renders IfNull SqlExpression
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected override void IfNull(StringBuilder builder, SqlExpression expr)
        {
            builder.Append("isnull(");
            if (expr.SubExpr1 is not null)
                Expression(builder, expr.SubExpr1);
            builder.Append(", ");
            if (expr.SubExpr2 is not null)
                Expression(builder, expr.SubExpr2);
            builder.Append(")");
        }

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

            builder.Append($"DATEDIFF({datePartValue},");
            Expression(builder, expression.SubExpr1);
            builder.Append(",");
            Expression(builder, expression.SubExpr2);
            builder.Append(")");
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
            // If there should be paging, create it first
            SelectQuery workingQuery = (query.PageIndex >= 0 && query.PageSize > 0) ? Paging(query) : query;
            workingQuery.Validate();

            StringBuilder selectBuilder = new();

            //Start the select statement
            Select(selectBuilder, workingQuery.Distinct);

            //Render Top clause
            if (workingQuery.Top > -1)
                selectBuilder.AppendFormat("top {0} ", workingQuery.Top);

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

            return selectBuilder.ToString();
        }

        public SelectQuery Paging(SelectQuery query)
        {
            // First create a clone of the query without any paging
            SelectQuery clone = query.Clone();
            clone.SetPaging(0, 0);

            // Wrap the query with a paged query
            SelectQuery wrapper = new(FromTerm.SubQuery(clone, "wrapper"));

            // Add a rowcount column to the query using the same ordering as the query
            StringBuilder columns = new();
            foreach (OrderByTerm orderByTerm in clone.OrderByTerms)
                OrderByTerm(columns, new OrderByTerm(orderByTerm.Field, wrapper.FromClause.BaseTable, orderByTerm.Direction));

            wrapper.Columns.Add(new SelectColumn("*", null));
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
            FromTerm wrapperTable = wrapperRowFilter.FromClause.BaseTable;
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
            SelectColumn col = new SelectColumn("*", null, "cnt", SqlAggregationFunction.Count);
            countQuery.Columns.Add(col);
            countQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "t");
            return RenderSelect(countQuery);
        }

        /*
            /// <summary>
            /// Renders a SELECT statement which a result-set page
            /// </summary>
            /// <param name="pageIndex">The zero based index of the page to be returned</param>
            /// <param name="pageSize">The size of a page</param>
            /// <param name="totalRowCount">Total number of rows the query would yeild if not paged</param>
            /// <param name="query">Query definition to apply paging on</param>
            /// <returns>Generated SQL statement</returns>
            /// <remarks>
            /// To generate pagination SQL on SqlServer 2000 you must supply <paramref name="totalRowCount"/>.
            /// To aquire the total number of rows use the <see cref="RenderRowCount"/> method.
            /// </remarks>
            public override string RenderPage(int pageIndex, int pageSize, int totalRowCount, SelectQuery query)
            {
              if (query.OrderByTerms.Count == 0)
                throw new InvalidQueryException("OrderBy must be specified for paging to work on SqlServer.");

              int currentPageSize = pageSize;
              if (pageSize * (pageIndex + 1) > totalRowCount)
                currentPageSize = totalRowCount - pageSize * pageIndex;

              SelectQuery baseQuery = query.Clone();

              baseQuery.Top = (pageIndex + 1) * pageSize;
              baseQuery.Columns.Clear();
              baseQuery.Columns.Add(new SelectColumn("*"));

              string baseSql = RenderSelect(baseQuery);

              SelectQuery reverseQuery = new SelectQuery();
              reverseQuery.Columns.Add(new SelectColumn("*"));
              reverseQuery.Top = currentPageSize;
              reverseQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "r");
              ApplyOrderBy(baseQuery.OrderByTerms, reverseQuery, false, reverseQuery.FromClause.BaseTable.Alias);
              string reverseSql = RenderSelect(reverseQuery);

              SelectQuery forwardQuery = new SelectQuery();
              forwardQuery.Columns.AddRange(query.Columns);
              forwardQuery.Top = currentPageSize;
              forwardQuery.FromClause.BaseTable = FromTerm.SubQuery(reverseSql, "f");
              ApplyOrderBy(baseQuery.OrderByTerms, forwardQuery, true, forwardQuery.FromClause.BaseTable.Alias);

              return RenderSelect(forwardQuery);
            }

            void ApplyOrderBy(OrderByTermCollection terms, SelectQuery orderQuery, bool forward, string tableAlias)
            {
              foreach(OrderByTerm expr in terms)
              {
                OrderByDirection dir = expr.Direction;

                //Reverse order direction if required
                if (!forward && dir == OrderByDirection.Ascending) 
                  dir = OrderByDirection.Descending;
                else if (!forward && dir == OrderByDirection.Descending) 
                  dir = OrderByDirection.Ascending;

                orderQuery.OrderByTerms.Add(new OrderByTerm(expr.Field.ToString(), FromTerm.TermRef(tableAlias) , dir));
              }
            }
        */
    }
