using System.Globalization;
using System.Text;

namespace Reeb.SqlOM.Render;

/// <summary>
/// Provides common implementation for ISqlOmRenderer
/// </summary>
public abstract class SqlOmRenderer : ISqlOmRenderer
{
    /// <summary>Date-only format string used by <see cref="Constant"/>.</summary>
    protected string dateFormat = "MM/dd/yyyy";
    /// <summary>Full timestamp format string used by <see cref="Constant"/>.</summary>
    protected string dateTimeFormat = "MM/dd/yyyy HH:mm:ss.fff";
    /// <summary>When true, renderers that support it may emit a read-uncommitted lock hint.</summary>
    protected bool readUncommitted = false;
    readonly char identifierOpeningQuote;
    readonly char identifierClosingQuote;

    /// <summary>Culture used for rendering numeric and date literal values.</summary>
    protected static readonly CultureInfo LiteralCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Creates a new SqlOmRenderer
        /// </summary>
        protected SqlOmRenderer(char identifierOpeningQuote, char identifierClosingQuote)
        {
            this.identifierOpeningQuote = identifierOpeningQuote;
            this.identifierClosingQuote = identifierClosingQuote;
        }

        /// <summary>
        /// Gets or sets a date format string
        /// </summary>
        /// <remarks>
        /// Use <see cref="DateFormat"/> to specify how date values should be formatted
        /// in order to be properly parsed by your database.
        /// Specific renderers set this property to the appliciable default value, so you
        /// only need to change this if your database is configured to use other then default date format.
        /// <para>
        /// DateFormat will be used to format <see cref="DateTime"/> values which have the Hour, Minute, Second and Milisecond properties set to 0.
        /// Otherwise, <see cref="DateTimeFormat"/> will be used.
        /// </para>
        /// </remarks>
        public string DateFormat
        {
            get { return dateFormat; }
            set { dateFormat = value; }
        }

        /// <summary>
        /// Gets or sets a date-time format string
        /// </summary>
        /// <remarks>
        /// Use <see cref="DateTimeFormat"/> to specify how timestamp values should be formatted
        /// in order to be properly parsed by your database.
        /// Specific renderers set this property to the appliciable default value, so you
        /// only need to change this if your database is configured to use other then default date format.
        /// </remarks>
        public string DateTimeFormat
        {
            get { return dateTimeFormat; }
            set { dateTimeFormat = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [read uncommitted] is allowed.
        /// </summary>
        /// <value><c>true</c> if [read uncommitted]; otherwise, <c>false</c>.</value>
        public bool ReadUncommitted
        {
            get { return readUncommitted; }
            set { readUncommitted = value; }
        }

        /// <summary>
        /// Renders a SELECT statement
        /// </summary>
        /// <param name="query">Query definition</param>
        /// <returns>Generated SQL statement</returns>
        public abstract string RenderSelect(SelectQuery query);

        /// <summary>
        /// Renders a row count SELECT statement. 
        /// </summary>
        /// <param name="query">Query definition to count rows for</param>
        /// <returns>Generated SQL statement</returns>
        public abstract string RenderRowCount(SelectQuery query);

        /// <summary>
        /// Specifies weather all identifiers should be converted to upper case while rendering
        /// </summary>
        protected virtual bool UpperCaseIdentifiers { get { return false; } }
        //bool IClauseRendererContext.UpperCaseIdentifiers { get {  return this.UpperCaseIdentifiers; } }

        /// <summary>
        /// Renders an UPDATE statement
        /// </summary>
        /// <param name="query">UPDATE query definition</param>
        /// <returns>Generated SQL statement</returns>
        public virtual string RenderUpdate(UpdateQuery query)
        {
            return UpdateStatement(query);
        }

        /// <summary>
        /// Renders an INSERT statement
        /// </summary>
        /// <param name="query">INSERT query definition</param>
        /// <returns>Generated SQL statement</returns>
        public virtual string RenderInsert(InsertQuery query)
        {
            return InsertStatement(query);
        }

        /// <summary>
        /// Renders an BULK INSERT statement
        /// </summary>
        /// <param name="query">INSERT query definition</param>
        /// <returns>Generated SQL statement</returns>
        public string RenderBulkInsert(BulkInsertQuery query)
        {
            return BulkInsertStatement(query);
        }

        /// <summary>
        /// Renders an DELETE statement
        /// </summary>
        /// <param name="query">DELETE query definition</param>
        /// <returns>Generated SQL statement</returns>
        public virtual string RenderDelete(DeleteQuery query)
        {
            return DeleteStatement(query);
        }


        /// <summary>
        /// Renders Common Table Expressions
        /// </summary>
        /// <param name="builder">String builder</param>
        /// <param name="ctes">CTE collection</param>
        protected virtual void RenderCommonTableExpressions(StringBuilder builder, CommonTableExpressionCollection ctes)
        {
            if (ctes.Count == 0) return;

            bool anyRecursive = false;
            for (int i = 0; i < ctes.Count; i++)
            {
                if (ctes[i].IsRecursive) { anyRecursive = true; break; }
            }

            builder.Append(anyRecursive ? "WITH RECURSIVE " : "WITH ");
            for (int i = 0; i < ctes.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                var cte = ctes[i];
                Identifier(builder, cte.Name, true);
                if (cte.ColumnNames != null && cte.ColumnNames.Length > 0)
                {
                    builder.Append(" (");
                    for (int j = 0; j < cte.ColumnNames.Length; j++)
                    {
                        if (j > 0)
                            builder.Append(", ");
                        Identifier(builder, cte.ColumnNames[j], true);
                    }
                    builder.Append(')');
                }
                builder.Append(" AS (");
                builder.Append(RenderSelect(cte.Query));
                builder.Append(')');
            }
            builder.Append(' ');
        }

        /// <summary>
        /// Renders a UNION clause
        /// </summary>
        /// <param name="union">Union definition</param>
        /// <returns>Generated SQL statement</returns>
        public virtual string RenderUnion(SqlUnion union)
        {
            StringBuilder builder = new();
            bool isFirst = true;
            foreach (SqlUnionItem item in union.Items)
            {
                if (!isFirst)
                    builder.Append(item.RepeatingAction == DistinctModifier.All ? " union all " : " union ");
                builder.Append(RenderSelect(item.Query));
                isFirst = false;
            }
            return builder.ToString();
        }

        /// <summary>
        /// Renders a SELECT statement which a result-set page
        /// </summary>
        /// <param name="pageIndex">The zero based index of the page to be returned</param>
        /// <param name="pageSize">The size of a page</param>
        /// <param name="totalRowCount">Total number of rows the query would yeild if not paged</param>
        /// <param name="query">Query definition to apply paging on</param>
        /// <returns>Generated SQL statement</returns>
        /// <remarks>
        /// To generate pagination SQL you must supply <paramref name="totalRowCount"/>.
        /// To aquire the total number of rows use the <see cref="RenderRowCount"/> method.
        /// </remarks>
        public virtual string RenderPage(int pageIndex, int pageSize, int totalRowCount, SelectQuery query)
        {
            if (query.OrderByTerms.Count == 0)
                throw new InvalidQueryException("OrderBy must be specified for paging to work on SqlServer.");

            int currentPageSize = pageSize;
            if (pageSize * (pageIndex + 1) > totalRowCount)
                currentPageSize = totalRowCount - pageSize * pageIndex;
            if (currentPageSize < 0)
                currentPageSize = 0;

            SelectQuery baseQuery = query.Clone();

            baseQuery.Top = (pageIndex + 1) * pageSize;
            //baseQuery.Columns.Add(new SelectColumn("*"));
            foreach (OrderByTerm term in baseQuery.OrderByTerms)
                baseQuery.Columns.Add(new SelectColumn(term.Field, term.Table, FormatSortFieldName(term.Field), SqlAggregationFunction.None));

            string baseSql = RenderSelect(baseQuery);

            SelectQuery reverseQuery = new SelectQuery();
            reverseQuery.Columns.Add(new SelectColumn("*"));
            reverseQuery.Top = currentPageSize;
            reverseQuery.FromClause.BaseTable = FromTerm.SubQuery(baseSql, "r");
            ApplyOrderBy(baseQuery.OrderByTerms, reverseQuery, false, reverseQuery.FromClause.BaseTable);
            string reverseSql = RenderSelect(reverseQuery);

            SelectQuery forwardQuery = new SelectQuery();
            forwardQuery.Columns.AddRange(query.Columns);
            forwardQuery.FromClause.BaseTable = FromTerm.SubQuery(reverseSql, "f");
            ApplyOrderBy(baseQuery.OrderByTerms, forwardQuery, true, forwardQuery.FromClause.BaseTable);

            return RenderSelect(forwardQuery);
        }

        string FormatSortFieldName(string fieldName)
        {
            return "sort_" + fieldName;
        }

        void ApplyOrderBy(OrderByTermCollection terms, SelectQuery orderQuery, bool forward, FromTerm table)
        {
            foreach (OrderByTerm expr in terms)
            {
                OrderByDirection dir = expr.Direction;

                //Reverse order direction if required
                if (!forward && dir == OrderByDirection.Ascending)
                    dir = OrderByDirection.Descending;
                else if (!forward && dir == OrderByDirection.Descending)
                    dir = OrderByDirection.Ascending;

                orderQuery.OrderByTerms.Add(new OrderByTerm(FormatSortFieldName(expr.Field.ToString()), table, dir));
            }
        }

        //protected abstract void SelectStatement(StringBuilder builder);

        /// <summary>
        /// Renders a the beginning of a SELECT clause with an optional DISTINCT setting
        /// </summary>
        /// <param name="builder">Select statement string builder</param>
        /// <param name="distinct">Turns on or off SQL distinct option</param>
        protected virtual void Select(StringBuilder builder, bool distinct)
        {
            builder.Append("select ");
            if (distinct)
                builder.Append("distinct ");
        }

        /// <summary>
        /// Renders columns of SELECT clause
        /// </summary>
        protected virtual void SelectColumns(StringBuilder builder, SelectColumnCollection columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    Coma(builder);
                SelectColumn(builder, columns[i]);
            }
        }

        /// <summary>
        /// Renders a single select column
        /// </summary>
        protected virtual void SelectColumn(StringBuilder builder, SelectColumn col)
        {
            Expression(builder, col.Expression);
            if (col.ColumnAlias != null)
            {
                builder.Append(' ');
                Identifier(builder, col.ColumnAlias);
            }
        }

        /// <summary>
        /// Renders a separator between select columns
        /// </summary>
        protected virtual void Coma(StringBuilder builder)
        {
            builder.Append(", ");
        }

        /// <summary>
        /// Renders the begining of a FROM clause
        /// </summary>
        /// <param name="builder"></param>
        protected virtual void From(StringBuilder builder)
        {
            builder.Append(" from ");
        }

        /// <summary>
        /// Renders the terms of a from clause
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="fromClause"></param>
        /// <param name="tableSpace">Common prefix for all tables in the clause</param>
        protected virtual void FromClause(StringBuilder builder, FromClause fromClause, string tableSpace)
        {
            From(builder);
            RenderFromTerm(builder, fromClause.BaseTable, tableSpace);

            foreach (Join join in fromClause.Joins)
            {
                builder.Append(' ');
                builder.Append(join.Type.ToString().ToLowerInvariant());
                builder.Append(" join ");
                RenderFromTerm(builder, join.RightTable, tableSpace);

                if (join.Type != JoinType.Cross)
                {
                    builder.Append(" on ");
                    WhereClause(builder, join.Conditions);
                }
            }
        }

        /// <summary>
        /// Renders a single FROM term
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="table"></param>
        /// <param name="tableSpace">Common prefix for all tables in the term</param>
        protected virtual void RenderFromTerm(StringBuilder builder, FromTerm table, string tableSpace)
        {
            if (table.Type == FromTermType.Table)
            {
                if (table.Ns1 != null)
                    TableNamespace(builder, table.Ns1);
                if (table.Ns2 != null)
                    TableNamespace(builder, table.Ns2);
                if (table.Ns1 == null && table.Ns2 == null && tableSpace != null)
                    TableNamespace(builder, tableSpace);
                string nameToRender = table.Expression != null ? (string)table.Expression : table.Alias!;
                Identifier(builder, nameToRender, table.RenderIdentifierQuotes);
            }
            else if (table.Type == FromTermType.SubQuery)
            {
                builder.Append("( ");
                builder.Append((string?)table.Expression);
                builder.Append(" )");
            }
            else if (table.Type == FromTermType.SubQueryObj && table.Expression is SelectQuery sq)
            {
                builder.Append("( ");
                builder.Append(RenderSelect(sq));
                builder.Append(" )");
            }
            else if (table.Type == FromTermType.SubQueryObj && table.Expression is SqlUnion union)
            {
                builder.Append("( ");
                builder.Append(RenderUnion(union));
                builder.Append(" )");
            }
            else
                throw new InvalidQueryException("Unknown FromExpressionType: " + table.Type);

            if (table.Alias != null)
            {
                builder.Append(' ');
                Identifier(builder, table.Alias, table.RenderIdentifierQuotes);
            }

            ApplyTableHints(builder, table);
        }

        /// <summary>
        /// Emits renderer-specific table hints (e.g. SQL Server's WITH (NOLOCK)).
        /// Base implementation only emits explicit <see cref="FromTerm.LockHint"/> hints; dialect-specific
        /// renderers override this to emit <see cref="ReadUncommitted"/> hints.
        /// </summary>
        protected virtual void ApplyTableHints(StringBuilder builder, FromTerm table)
        {
            if (table.LockHint != LockHintType.NONE)
            {
                builder.Append(" WITH (");
                builder.Append(table.LockHint);
                builder.Append(')');
            }
        }

        /// <summary>
        /// Renders the table namespace
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ns"></param>
        protected virtual void TableNamespace(StringBuilder builder, string ns)
        {
            builder.Append(ns);
            builder.Append('.');
        }

        /// <summary>
        /// Renders the begining of a WHERE statement
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="group"></param>
        protected virtual void Where(StringBuilder builder, WhereClause group)
        {
            if (group.IsEmpty)
                return;

            builder.Append(" where ");
        }

        /// <summary>
        /// Renders the begining of a HAVING statement
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="group"></param>
        protected virtual void Having(StringBuilder builder, WhereClause group)
        {
            if (group.IsEmpty)
                return;

            builder.Append(" having ");
        }

        /// <summary>
        /// Recursivly renders a WhereClause
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="group"></param>
        protected virtual void WhereClause(StringBuilder builder, WhereClause group)
        {
            if (group.IsEmpty)
                return;

            builder.Append('(');

            for (int i = 0; i < group.Terms.Count; i++)
            {
                if (i > 0)
                    RelationshipOperator(builder, group.Relationship);

                WhereTerm term = (WhereTerm)group.Terms[i];
                WhereClause(builder, term);
            }

            bool operatorRequired = group.Terms.Count > 0;
            foreach (WhereClause childGroup in group.SubClauses)
            {
                if (childGroup.IsEmpty)
                    continue;

                if (operatorRequired)
                    RelationshipOperator(builder, group.Relationship);

                WhereClause(builder, childGroup);
                operatorRequired = true;
            }

            builder.Append(')');
        }

        /// <summary>
        /// Renders bitwise and
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void BitwiseAnd(StringBuilder builder, WhereTerm term)
        {
            builder.Append("(");
            Expression(builder, term.Expr1);
            builder.Append(" & ");
            Expression(builder, term.Expr2);
            builder.Append(") > 0");
        }

        /// <summary>
        /// Renders a single WhereTerm
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void WhereClause(StringBuilder builder, WhereTerm term)
        {
            if (term.Type == WhereTermType.Compare && term.Op == CompareOperator.BitwiseAnd)
                BitwiseAnd(builder, term);
            else if (term.Type == WhereTermType.Compare)
            {
                // WhereTerm.CreateCompare auto-promotes null-Expr2 to WhereTermType.IsNull,
                // so Expr2 is guaranteed non-null here.
                Expression(builder, term.Expr1);
                builder.Append(' ');
                Operator(builder, term.Op);
                builder.Append(' ');
                Expression(builder, term.Expr2);
            }
            else if (term.Type == WhereTermType.In || term.Type == WhereTermType.NotIn || term.Type == WhereTermType.InSubQuery || term.Type == WhereTermType.NotInSubQuery || term.Type == WhereTermType.InSubQueryObj || term.Type == WhereTermType.NotInSubQueryObj)
            {
                Expression(builder, term.Expr1);
                if (term.Type == WhereTermType.NotIn || term.Type == WhereTermType.NotInSubQuery || term.Type == WhereTermType.NotInSubQueryObj)
                    builder.Append(" not");
                builder.Append(" in (");
                if (term.Type == WhereTermType.InSubQuery || term.Type == WhereTermType.NotInSubQuery)
                    builder.Append((string)term.SubQuery);
                else if (term.Type == WhereTermType.InSubQueryObj || term.Type == WhereTermType.NotInSubQueryObj)
                    builder.Append(RenderSelect((SelectQuery)term.SubQuery));
                else
                    ConstantList(builder, term.Values);
                builder.Append(')');
            }
            else if (term.Type == WhereTermType.Exists || term.Type == WhereTermType.ExistsObj || term.Type == WhereTermType.NotExists || term.Type == WhereTermType.NotExistsObj)
            {
                if (term.Type == WhereTermType.NotExists || term.Type == WhereTermType.NotExistsObj)
                    builder.Append(" not");
                builder.Append(" exists (");
                if (term.Type == WhereTermType.Exists || term.Type == WhereTermType.NotExists)
                    builder.Append((string)term.SubQuery);
                else
                    builder.Append(RenderSelect((SelectQuery)term.SubQuery));
                builder.Append(')');
            }
            else if (term.Type == WhereTermType.Raw)
            {
                builder.Append(' ');
                builder.Append((string)term.SubQuery);
            }
            else if (term.Type == WhereTermType.Between)
            {
                Expression(builder, term.Expr1);
                builder.Append(" between ");
                Expression(builder, term.Expr2);
                builder.Append(" and ");
                Expression(builder, term.Expr3);
            }
            else if (term.Type == WhereTermType.NotBetween)
            {
                Expression(builder, term.Expr1);
                builder.Append(" not between ");
                Expression(builder, term.Expr2);
                builder.Append(" and ");
                Expression(builder, term.Expr3);
            }
            else if (term.Type == WhereTermType.IsNull)
            {
                Expression(builder, term.Expr1);
                builder.Append(" is null ");
            }
            else if (term.Type == WhereTermType.IsNotNull)
            {
                Expression(builder, term.Expr1);
                builder.Append(" is not null ");
            }
        }

        /// <summary>
        /// Renders IfNull SqlExpression
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected abstract void IfNull(StringBuilder builder, SqlExpression expr);

        /// <summary>Renders the DATEDIFF expression.</summary>
        /// <remarks>The default implementation always throws an exception: implementation must occcur at the inheritor.</remarks>
        /// <param name="builder">The builder to append the expression to.</param>
        /// <param name="expression">The expression to be build.</param>
        /// <seealso cref="http://msdn.microsoft.com/en-us/library/ms189794.aspx"/>
        protected virtual void DateDiff(StringBuilder builder, SqlExpression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Renders SqlExpression
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected virtual void Expression(StringBuilder builder, SqlExpression expr)
        {
            SqlExpressionType type = expr.Type;
            if (type == SqlExpressionType.Field)
                QualifiedIdentifier(builder, expr.TableAlias, expr.Value?.ToString() ?? string.Empty);
            else if (type == SqlExpressionType.Function)
            {
                if (!expr.DatabaseFunction.Equals(SqlDatabaseFunction.None))
                    Function(builder, expr.DatabaseFunction, expr.SubExpr1);
                else
                    Function(builder, expr.AggFunction, expr.SubExpr1);
            }
            else if (type == SqlExpressionType.Constant)
                Constant(builder, (SqlConstant)expr.Value!);
            else if (type == SqlExpressionType.SubQueryText)
            {
                builder.Append('(');
                builder.Append((string)expr.Value!);
                builder.Append(')');
            }
            else if (type == SqlExpressionType.SubQueryObject)
            {
                builder.Append('(');
                builder.Append(RenderSelect((SelectQuery)expr.Value!));
                builder.Append(')');
            }
            else if (type == SqlExpressionType.PseudoField)
                builder.Append((string)expr.Value!);
            else if (type == SqlExpressionType.Parameter)
                builder.Append((string)expr.Value!);
            else if (type == SqlExpressionType.Raw)
                builder.Append((string)expr.Value!);
            else if (type == SqlExpressionType.Case)
                CaseClause(builder, expr.CaseClause);
            else if (type == SqlExpressionType.IfNull)
                IfNull(builder, expr);
            else if (type == SqlExpressionType.Null)
                builder.Append("null");
            else if (type == SqlExpressionType.DateDiff)
                DateDiff(builder, expr);
            else
                throw new InvalidQueryException($"Unknown expression type: {type}");
        }

        /// <summary>
        /// Renders a SqlExpression of type Function 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="func"></param>
        /// <param name="param"></param>
        protected virtual void Function(StringBuilder builder, SqlAggregationFunction func, SqlExpression? param)
        {
            builder.Append(func);
            builder.Append('(');
            if (param is not null)
                Expression(builder, param);
            builder.Append(')');
        }

        /// <summary>
        /// Renders a SqlExpression of type Function 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="func"></param>
        /// <param name="param"></param>
        protected virtual void Function(StringBuilder builder, SqlDatabaseFunction func, SqlExpression? param)
        {
            builder.Append(func);
            builder.Append('(');
            if (param is not null)
                Expression(builder, param);
            builder.Append(')');
        }

        /// <summary>
        /// Converts a byte array to the '0x...' binary SQL literal form.
        /// </summary>
        protected static string ByteArrayToHexString(byte[]? b)
        {
            if (b is null || b.Length == 0)
                return "0x";

#if NET5_0_OR_GREATER
            return string.Concat("0x", Convert.ToHexString(b));
#else
            StringBuilder sb = new(b.Length * 2 + 2);
            sb.Append("0x");
            foreach (byte val in b)
                sb.Append(val.ToString("X2", CultureInfo.InvariantCulture));
            return sb.ToString();
#endif
        }

        /// <summary>
        /// Renders a constant
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="expr"></param>
        protected virtual void Constant(StringBuilder builder, SqlConstant expr)
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
                builder.Append('\'');
                builder.Append(SqlEncode(expr.Value?.ToString() ?? string.Empty));
                builder.Append('\'');
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

        /// <summary>
        /// Renders a comparison operator
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="op"></param>
        protected virtual void Operator(StringBuilder builder, CompareOperator op)
        {
            string operatorString = op switch
            {
                CompareOperator.Equal => "=",
                CompareOperator.NotEqual => "<>",
                CompareOperator.Greater => ">",
                CompareOperator.Less => "<",
                CompareOperator.LessOrEqual => "<=",
                CompareOperator.GreaterOrEqual => ">=",
                CompareOperator.Like or CompareOperator.StartsWith or CompareOperator.EndsWith => "like",
                CompareOperator.NotLike or CompareOperator.NotStartsWith or CompareOperator.NotEndsWith => "not like",
                _ => throw new InvalidQueryException($"Unknown operator: {op}")
            };
            builder.Append(operatorString);
        }

        /// <summary>
        /// Renders a list of values
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected virtual void ConstantList(StringBuilder builder, SqlConstantCollection values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SqlConstant val = values[i];
                Constant(builder, val);
                if (i != values.Count - 1)
                    Coma(builder);
            }
        }

        /// <summary>
        /// Encodes a textual string.
        /// </summary>
        /// <param name="val">Text to be encoded</param>
        /// <returns>Encoded text</returns>
        /// <remarks>All text string must be encoded before they are appended to a SQL statement.</remarks>
        public virtual string SqlEncode(string val)
        {
            return val.Replace("'", "''");
        }

        /// <summary>
        /// Renders a relationship operator
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="relationship"></param>
        protected virtual void RelationshipOperator(StringBuilder builder, WhereClauseRelationship relationship)
        {
            builder.Append(' ');
            builder.Append(relationship.ToString().ToLowerInvariant());
            builder.Append(' ');
        }

        /// <summary>
        /// Renders the begining of a GROUP BY statement.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="groupByTerms"></param>
        /// <remarks>If <paramref name="groupByTerms"/> has no items, nothing will be appended.</remarks>
        protected virtual void GroupBy(StringBuilder builder, GroupByTermCollection groupByTerms)
        {
            if (groupByTerms.Count > 0)
                builder.Append(" group by ");
        }

        /// <summary>
        /// Renders GROUP BY terms 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="groupByTerms"></param>
        protected virtual void GroupByTerms(StringBuilder builder, GroupByTermCollection groupByTerms)
        {
            for (int i = 0; i < groupByTerms.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                GroupByTerm(builder, groupByTerms[i]);
            }
        }

        /// <summary>
        /// Renders a single GROUP BY term
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void GroupByTerm(StringBuilder builder, GroupByTerm term)
        {
            QualifiedIdentifier(builder, term.TableAlias, term.Field);
        }

        /// <summary>
        /// Renders the begining of a ORDER BY statement.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="orderByTerms"></param>
        /// <remarks>If <paramref name="orderByTerms"/> has no items, nothing will be appended.</remarks>
        protected virtual void OrderBy(StringBuilder builder, OrderByTermCollection orderByTerms)
        {
            if (orderByTerms.Count > 0)
                builder.Append(" order by ");
        }

        /// <summary>
        /// Renders ORDER BY terms
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="orderByTerms"></param>
        protected virtual void OrderByTerms(StringBuilder builder, OrderByTermCollection orderByTerms)
        {
            for (int i = 0; i < orderByTerms.Count; i++)
            {
                OrderByTerm term = (OrderByTerm)orderByTerms[i];
                if (i > 0)
                    builder.Append(", ");

                OrderByTerm(builder, term);
            }
        }

        /// <summary>
        /// Renders a single ORDER BY term
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void OrderByTerm(StringBuilder builder, OrderByTerm term)
        {
            QualifiedIdentifier(builder, term.TableAlias, term.Field);
            builder.Append(term.Direction == OrderByDirection.Descending ? " desc" : " asc");
        }

        /// <summary>
        /// Renders an identifier name.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="name">Identifier name</param>
        protected virtual void Identifier(StringBuilder builder, string name)
        {
            Identifier(builder, name, true);
        }

        /// <summary>
        /// Renders an identifier name.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="name">Identifier name</param>
        /// <param name="renderIdentifierQuotes"></param>
        protected virtual void Identifier(StringBuilder builder, string name, bool renderIdentifierQuotes)
        {
            // Functions with parameters must not be enclosed in identifier quotes.
            if (name == "*" || (name.EndsWith(')') && name.LastIndexOf('(') > 0))
            {
                builder.Append(name);
                return;
            }

            if (UpperCaseIdentifiers)
                name = name.ToUpperInvariant();

            if (renderIdentifierQuotes)
            {
                builder.Append(identifierOpeningQuote);
                builder.Append(name);
                builder.Append(identifierClosingQuote);
            }
            else
                builder.Append(name);
        }

        /// <summary>
        /// Renders a fully qualified identifer.
        /// </summary>
        /// <param name="builder">Select statement string builder</param>
        /// <param name="qnamespace">Identifier namespace</param>
        /// <param name="name">Identifier name</param>
        /// <remarks>
        /// <see cref="QualifiedIdentifier"/> is usually to render database fields with optional table alias prefixes.
        /// <paramref name="name"/> is a mandatory parameter while <paramref name="qnamespace"/> is optional.
        /// If <paramref name="qnamespace"/> is null, identifier will be rendered without a namespace (aka table alias)
        /// </remarks>
        protected virtual void QualifiedIdentifier(StringBuilder builder, string? qnamespace, string name)
        {
            if (qnamespace is not null)
            {
                Identifier(builder, qnamespace);
                builder.Append('.');
            }

            Identifier(builder, name);
        }

        /// <summary>
        /// Renders a the beginning of an UPDATE clause with the table name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="tableName">Name of the table to be updated</param>
        protected virtual void Update(StringBuilder builder, string tableName)
        {
            builder.Append("update ");
            Identifier(builder, tableName);
            builder.Append(" set ");
        }

        /// <summary>
        /// Renders update phrases
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="terms"></param>
        protected virtual void UpdateTerms(StringBuilder builder, UpdateTermCollection terms)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                if (i > 0)
                    Coma(builder);
                UpdateTerm(builder, terms[i]);
            }
        }

        /// <summary>
        /// Render a single update phrase
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void UpdateTerm(StringBuilder builder, UpdateTerm term)
        {
            Identifier(builder, term.FieldName);
            builder.Append(" = ");
            Expression(builder, term.Value);
        }

        /// <summary>
        /// Renders the whole UPDATE statement using ANSI SQL standard
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The rendered SQL string</returns>
        public virtual string UpdateStatement(UpdateQuery query)
        {
            query.Validate();
            StringBuilder builder = new();
            Update(builder, query.TableName);
            UpdateTerms(builder, query.Terms);
            Where(builder, query.WhereClause);
            WhereClause(builder, query.WhereClause);

            return builder.ToString();
        }

        /// <summary>
        /// Render the beginning of an INSERT statement with table name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="tableName"></param>
        protected virtual void Insert(StringBuilder builder, string tableName)
        {
            builder.Append("insert into ");
            Identifier(builder, tableName);
            builder.Append(" ");
        }

        /// <summary>
        /// Render the list of columns which are to be inserted.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="terms"></param>
        protected virtual void InsertColumns(StringBuilder builder, UpdateTermCollection terms)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                if (i > 0)
                    Coma(builder);
                InsertColumn(builder, terms[i]);
            }
        }

        /// <summary>
        /// Render a single column name in an INSERT statement
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void InsertColumn(StringBuilder builder, UpdateTerm term)
        {
            Identifier(builder, term.FieldName);
        }

        /// <summary>
        /// Render the values of an INSERT statement
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="terms"></param>
        protected virtual void InsertValues(StringBuilder builder, UpdateTermCollection terms)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                if (i > 0)
                    Coma(builder);
                InsertValue(builder, terms[i]);
            }
        }

        /// <summary>
        /// Render a single INSERT value
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void InsertValue(StringBuilder builder, UpdateTerm term)
        {
            Expression(builder, term.Value);
        }

        /// <summary>
        /// Render the whole INSERT statement in ANSI standard
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The rendered SQL INSERT statement</returns>
        public virtual string InsertStatement(InsertQuery query)
        {
            query.Validate();
            StringBuilder builder = new();
            Insert(builder, query.TableName);

            builder.Append('(');
            InsertColumns(builder, query.Terms);
            builder.Append(") values (");
            InsertValues(builder, query.Terms);
            builder.Append(')');
            return builder.ToString();
        }

        /// <summary>
        /// Render the beginning of a DELETE statement
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="tableName"></param>
        protected virtual void Delete(StringBuilder builder, string tableName)
        {
            builder.Append("delete from ");
            Identifier(builder, tableName);
            builder.Append(" ");
        }

        /// <summary>
        /// Render the whole DELETE statement in ANSI SQL standard
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The rendered SQL statement</returns>
        public virtual string DeleteStatement(DeleteQuery query)
        {
            query.Validate();
            StringBuilder builder = new();
            Delete(builder, query.TableName);
            Where(builder, query.WhereClause);
            WhereClause(builder, query.WhereClause);
            return builder.ToString();
        }

        /// <summary>
        /// Renders a CaseClause
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clause"></param>
        protected virtual void CaseClause(StringBuilder builder, CaseClause clause)
        {
            builder.Append(" case ");
            foreach (CaseTerm term in clause.Terms)
                CaseTerm(builder, term);
            if (clause.ElseValue != null)
            {
                builder.Append(" else ");
                Expression(builder, clause.ElseValue);
            }

            builder.Append(" end ");
        }

        /// <summary>
        /// Renders a CaseTerm
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="term"></param>
        protected virtual void CaseTerm(StringBuilder builder, CaseTerm term)
        {
            builder.Append(" when ");
            WhereClause(builder, term.Condition);
            builder.Append(" then ");
            Expression(builder, term.Value);
        }


        /// <summary>
        /// Render the whole BULK INSERT statement in ANSI standard
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The rendered SQL INSERT statement</returns>
        public virtual string BulkInsertStatement(BulkInsertQuery query)
        {
            query.Validate();
            StringBuilder builder = new();
            Insert(builder, query.TableName);

            builder.Append('(');
            // First build the columns using the first query (since all fields should be the same).
            InsertColumns(builder, query.Terms[0].Terms);
            builder.Append(") values ");

            // Next build the values
            for (int i = 0; i < query.Terms.Count; i++)
            {
                if (i > 0)
                    Coma(builder);

                builder.Append('(');
                InsertValues(builder, query.Terms[i].Terms);
                builder.Append(')');
            }

            return builder.ToString();
        }
    }
