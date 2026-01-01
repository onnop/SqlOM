using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render
{
    /// <summary>
    /// Renderer for SqlServer
    /// </summary>
    /// <remarks>
    /// Use SQLiteRenderer to render SQL statements for SQLite database.
    /// This version of Sql.Net has been tested with SQLite 3.5
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

        /// <summary>
        /// Renders a constant
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected override void Constant(StringBuilder builder, SqlConstant expr)
        {
            SqlDataType type = expr.Type;

            if (type == SqlDataType.Boolean)
                builder.Append(((bool)expr.Value) ? "1" : "0");
            if (type == SqlDataType.Number)
                builder.AppendFormat(new CultureInfo("en-US"), "{0}", expr.Value);
            else if (type == SqlDataType.Guid)
                builder.AppendFormat("'{0}'", expr.Value.ToString());
            else if (type == SqlDataType.Binary)
                builder.Append(ByteArrayToHexString((byte[])expr.Value));
            else if (type == SqlDataType.String)
            {
                if (expr.Value == null)
                    builder.AppendFormat("{0}", "null");
                else
                    builder.AppendFormat("'{0}'", expr.Value.ToString());
            }
            else if (type == SqlDataType.Date)
            {
                DateTime val = (DateTime)expr.Value;
                bool dateOnly = (val.Hour == 0 && val.Minute == 0 && val.Second == 0 && val.Millisecond == 0);
                string format = (dateOnly) ? dateFormat : dateTimeFormat;
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
            return RenderSelect(query, true);
        }

        string RenderSelect(SelectQuery query, bool renderOrderBy)
        {
            query.Validate();

            StringBuilder selectBuilder = new StringBuilder();

            //Start the select statement
            this.Select(selectBuilder, query.Distinct);

            //Render select columns
            this.SelectColumns(selectBuilder, query.Columns);

            this.FromClause(selectBuilder, query.FromClause, query.TableSpace);

            this.Where(selectBuilder, query.WherePhrase);
            this.WhereClause(selectBuilder, query.WherePhrase);

            this.GroupBy(selectBuilder, query.GroupByTerms);
            this.GroupByTerms(selectBuilder, query.GroupByTerms);

            if (query.GroupByWithCube)
                selectBuilder.Append(" with cube");
            else if (query.GroupByWithRollup)
                selectBuilder.Append(" with rollup");

            this.Having(selectBuilder, query.HavingPhrase);
            this.WhereClause(selectBuilder, query.HavingPhrase);

            if (renderOrderBy)
            {
                this.OrderBy(selectBuilder, query.OrderByTerms);
                this.OrderByTerms(selectBuilder, query.OrderByTerms);
            }

            //Render Top clause
            if (query.Top > -1)
                selectBuilder.AppendFormat(" limit {0}", query.Top);

            return selectBuilder.ToString();
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

    }
}
