using Reeb.SqlOM;
using Reeb.SqlOM.Render;

internal class Program
{
    static void Main(string[] args)
    {

// Test 1: Basic SELECT query
Console.WriteLine("1. Basic SELECT Query:");
var query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("email"));
query.FromClause.BaseTable = FromTerm.Table("customers");

var renderer = new SqlServerRenderer();
var sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 2: WHERE clause
Console.WriteLine("2. SELECT with WHERE Clause:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.FromClause.BaseTable = FromTerm.Table("customers");
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("age", FromTerm.Table("customers")),
    SqlExpression.Number(30),
    CompareOperator.Greater));

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 3: JOIN
Console.WriteLine("3. SELECT with JOIN:");
var tCustomers = FromTerm.Table("customers", "c");
var tOrders = FromTerm.Table("orders", "o");

query = new SelectQuery();
query.Columns.Add(new SelectColumn("c.name"));
query.Columns.Add(new SelectColumn("o.amount"));
query.FromClause.BaseTable = tCustomers;
query.FromClause.Join(JoinType.Inner, tCustomers, tOrders, "id", "customer_id");

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 4: ORDER BY
Console.WriteLine("4. SELECT with ORDER BY:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("total_purchases"));
query.FromClause.BaseTable = FromTerm.Table("customers");
query.OrderByTerms.Add(new OrderByTerm("total_purchases", OrderByDirection.Descending));

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 5: GROUP BY and Aggregation
Console.WriteLine("5. SELECT with GROUP BY and Aggregation:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("city"));
query.Columns.Add(new SelectColumn(SqlExpression.Raw("COUNT(*)"), "customer_count"));
query.Columns.Add(new SelectColumn(SqlExpression.Raw("SUM(total_purchases)"), "total_sales"));
query.FromClause.BaseTable = FromTerm.Table("customers");
query.GroupByTerms.Add(new GroupByTerm("city"));

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 6: UNION
Console.WriteLine("6. UNION Queries:");
var query1 = new SelectQuery();
query1.Columns.Add(new SelectColumn("name"));
query1.FromClause.BaseTable = FromTerm.Table("customers");
query1.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("city"), SqlExpression.String("New York"), CompareOperator.Equal));

var query2 = new SelectQuery();
query2.Columns.Add(new SelectColumn("name"));
query2.FromClause.BaseTable = FromTerm.Table("customers");
query2.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("city"), SqlExpression.String("Chicago"), CompareOperator.Equal));

var union = new SqlUnion();
union.Add(query1);
union.Add(query2);

sql = renderer.RenderUnion(union);
Console.WriteLine(sql);
Console.WriteLine();

// Test 7: Common Table Expression (CTE)
Console.WriteLine("7. Common Table Expression (CTE):");
var cteQuery = new SelectQuery();
cteQuery.Columns.Add(new SelectColumn("customer_id"));
cteQuery.Columns.Add(new SelectColumn(SqlExpression.Raw("SUM(amount)"), "total_orders"));
cteQuery.FromClause.BaseTable = FromTerm.Table("orders");
cteQuery.GroupByTerms.Add(new GroupByTerm("customer_id"));

var cte = new CommonTableExpression("customer_totals", cteQuery);

var mainQuery = new SelectQuery();
mainQuery.CommonTableExpressions.Add(cte);
mainQuery.Columns.Add(new SelectColumn("c.name"));
mainQuery.Columns.Add(new SelectColumn("ct.total_orders"));
mainQuery.FromClause.BaseTable = FromTerm.Table("customers", "c");
mainQuery.FromClause.Join(JoinType.Left, FromTerm.Table("customers", "c"), FromTerm.Table("customer_totals", "ct"), "id", "customer_id");

sql = renderer.RenderSelect(mainQuery);
Console.WriteLine(sql);
Console.WriteLine();

// Test 8: CASE Expression
Console.WriteLine("8. CASE Expression:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("total_purchases"));

var caseClause = new CaseClause();
var condition = new WhereClause(WhereClauseRelationship.And);
condition.Terms.Add(WhereTerm.CreateCompare(SqlExpression.Field("total_purchases"), SqlExpression.Number(2000), CompareOperator.Greater));
caseClause.Terms.Add(new CaseTerm(condition, SqlExpression.String("High Value")));
caseClause.ElseValue = SqlExpression.String("Regular");

query.Columns.Add(new SelectColumn(SqlExpression.Case(caseClause), "value_category"));
query.FromClause.BaseTable = FromTerm.Table("customers");

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 9: Parameterized Query
Console.WriteLine("9. Parameterized Query:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.FromClause.BaseTable = FromTerm.Table("customers");
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Parameter("@minAge"),
    SqlExpression.Field("age"),
    CompareOperator.LessOrEqual));

sql = renderer.RenderSelect(query);
Console.WriteLine(sql);
Console.WriteLine();

// Test 10: Fluent API
Console.WriteLine("10. Fluent API:");
var table = FromTerm.Table<Customer>();
var fluentQuery = SelectQuery.For<Customer>()
    .Where<Customer>(c => c.Age, table, CompareOperator.Greater, SqlExpression.Number(25))
    .OrderBy("total_purchases", table, OrderByDirection.Descending)
    .Select<Customer>(c => c.Name, table)
    .Select<Customer>(c => c.Email, table);

sql = renderer.RenderSelect(fluentQuery);
Console.WriteLine(sql);
Console.WriteLine();

// Test 11: Multiple Renderers
Console.WriteLine("11. Multiple Database Renderers:");
query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.FromClause.BaseTable = FromTerm.Table("customers");

Console.WriteLine("SQL Server:");
Console.WriteLine(new SqlServerRenderer().RenderSelect(query));

Console.WriteLine("MySQL:");
Console.WriteLine(new MySqlRenderer().RenderSelect(query));

Console.WriteLine("SQLite:");
Console.WriteLine(new SQLiteRenderer().RenderSelect(query));

Console.WriteLine("PostgreSQL:");
Console.WriteLine(new PostgreSqlRenderer().RenderSelect(query));

Console.WriteLine("Oracle:");
Console.WriteLine(new OracleRenderer().RenderSelect(query));
Console.WriteLine();

// Test 12: Strongly-typed Query Building
Console.WriteLine("12. Strongly-typed Query Building:");
var typedQuery = SqlOMExtensions.GenerateSelectQuery<Customer>();
typedQuery.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("age"), SqlExpression.Number(30), CompareOperator.Greater));

sql = renderer.RenderSelect(typedQuery);
Console.WriteLine(sql);
Console.WriteLine();

Console.WriteLine("=== All Tests Completed Successfully! ===");
    }
}

// Test model classes
[TableName("customers")]
public class Customer
{
    [ColumnName("id")]
    public int Id { get; set; }

    [ColumnName("name")]
    public string Name { get; set; } = string.Empty;

    [ColumnName("email")]
    public string Email { get; set; } = string.Empty;

    [ColumnName("age")]
    public int Age { get; set; }

    [ColumnName("city")]
    public string City { get; set; } = string.Empty;

    [ColumnName("total_purchases")]
    public decimal TotalPurchases { get; set; }
}

[TableName("orders")]
public class Order
{
    [ColumnName("id")]
    public int Id { get; set; }

    [ColumnName("customer_id")]
    public int CustomerId { get; set; }

    [ColumnName("order_date")]
    public DateTime OrderDate { get; set; }

    [ColumnName("amount")]
    public decimal Amount { get; set; }

    [ColumnName("status")]
    public string Status { get; set; } = string.Empty;
}

