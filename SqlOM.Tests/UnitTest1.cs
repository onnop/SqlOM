using Microsoft.Data.Sqlite;
using Reeb.SqlOM;
using Reeb.SqlOM.Render;
using Xunit;

namespace SqlOM.Tests;

public class BasicTests
{
    [Fact]
    public void TestBasicSelectQueryCreation()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("email"));
        query.FromClause.BaseTable = FromTerm.Table("customers");

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("select", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[name]", sql);
        Assert.Contains("[email]", sql);
        Assert.Contains("[customers]", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestWhereClause()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
            SqlExpression.Field("age", FromTerm.Table("customers")),
            SqlExpression.Number(30),
            CompareOperator.Greater));

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("where", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("age", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestJoin()
    {
        var tCustomers = FromTerm.Table("customers", "c");
        var tOrders = FromTerm.Table("orders", "o");

        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("c.name"));
        query.Columns.Add(new SelectColumn("o.amount"));
        query.FromClause.BaseTable = tCustomers;
        query.FromClause.Join(JoinType.Inner, tCustomers, tOrders, "id", "customer_id");

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("inner join", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("customers", sql);
        Assert.Contains("orders", sql);
    }

    [Fact]
    public void TestOrderBy()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("total_purchases"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.OrderByTerms.Add(new OrderByTerm("total_purchases", OrderByDirection.Descending));

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("order by", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("desc", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestGroupByAndAggregation()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("city"));
        query.Columns.Add(new SelectColumn(SqlExpression.Raw("COUNT(*)"), "customer_count"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.GroupByTerms.Add(new GroupByTerm("city"));

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("group by", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(*)", sql);
    }

    [Fact]
    public void TestUnion()
    {
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

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderUnion(union);

        Assert.Contains("union", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestCommonTableExpression()
    {
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
        var customersAlias = FromTerm.Table("customers", "c");
        var customerTotalsAlias = FromTerm.Table("customer_totals", "ct");
        mainQuery.FromClause.BaseTable = customersAlias;
        mainQuery.FromClause.Join(JoinType.Left, customersAlias, customerTotalsAlias, "id", "customer_id");

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(mainQuery);

        Assert.Contains("with", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("customer_totals", sql);
    }

    [Fact]
    public void TestCaseExpression()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("total_purchases"));

        var caseClause = new CaseClause();
        var caseCondition = new WhereClause(WhereClauseRelationship.And);
        caseCondition.Terms.Add(WhereTerm.CreateCompare(
            SqlExpression.Field("total_purchases"), SqlExpression.Number(2000), CompareOperator.Greater));
        caseClause.Terms.Add(new CaseTerm(caseCondition, SqlExpression.String("High Value")));
        caseClause.ElseValue = SqlExpression.String("Regular");

        query.Columns.Add(new SelectColumn(SqlExpression.Case(caseClause), "value_category"));
        query.FromClause.BaseTable = FromTerm.Table("customers");

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("case", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("when", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("then", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("else", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("end", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestParameterizedQuery()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
            SqlExpression.Parameter("@minAge"),
            SqlExpression.Field("age"),
            CompareOperator.LessOrEqual));

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("@minAge", sql);
    }

    [Fact]
    public void TestMultipleRenderers()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.FromClause.BaseTable = FromTerm.Table("customers");

        var sqlServerSql = new SqlServerRenderer().RenderSelect(query);
        Assert.Contains("customers", sqlServerSql, StringComparison.OrdinalIgnoreCase);

        var mysqlSql = new MySqlRenderer().RenderSelect(query);
        Assert.Contains("customers", mysqlSql, StringComparison.OrdinalIgnoreCase);

        var sqliteSql = new SQLiteRenderer().RenderSelect(query);
        Assert.Contains("customers", sqliteSql, StringComparison.OrdinalIgnoreCase);

        var oracleSql = new OracleRenderer().RenderSelect(query);
        Assert.Contains("customers", oracleSql, StringComparison.OrdinalIgnoreCase);

        var postgresSql = new PostgreSqlRenderer().RenderSelect(query);
        Assert.Contains("customers", postgresSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestFluentApi()
    {
        var table = FromTerm.Table<Customer>("c");
        var query = new SelectQuery(table)
            .Select<Customer>(c => c.Name, table)
            .Select<Customer>(c => c.Email, table)
            .Where<Customer>(c => c.Age, table, CompareOperator.Greater, SqlExpression.Number(25))
            .OrderBy<Customer>(c => c.TotalPurchases, table, OrderByDirection.Descending);

        var renderer = new SqlServerRenderer();
        var sql = renderer.RenderSelect(query);

        Assert.Contains("where", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by", sql, StringComparison.OrdinalIgnoreCase);
    }
}

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

public class InMemorySQLiteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SQLiteRenderer _renderer = new();

    public InMemorySQLiteTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER,
                city TEXT,
                total_purchases DECIMAL DEFAULT 0
            );
            
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER NOT NULL,
                order_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                amount DECIMAL NOT NULL,
                status TEXT,
                FOREIGN KEY(customer_id) REFERENCES customers(id)
            );
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            INSERT INTO customers (name, email, age, city, total_purchases) VALUES
            ('John Doe', 'john@example.com', 28, 'New York', 2500),
            ('Jane Smith', 'jane@example.com', 35, 'Chicago', 1800),
            ('Bob Johnson', 'bob@example.com', 22, 'New York', 500);
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            INSERT INTO orders (customer_id, amount, status) VALUES
            (1, 500, 'completed'),
            (1, 1200, 'completed'),
            (1, 800, 'pending'),
            (2, 300, 'completed'),
            (2, 700, 'completed'),
            (3, 150, 'completed');
        ";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void TestSelectAllCustomers()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("email"));
        query.FromClause.BaseTable = FromTerm.Table("customers");

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var count = 0;
        while (results.Read())
            count++;

        Assert.Equal(3, count);
    }

    [Fact]
    public void TestSelectWithWhere()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("total_purchases"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
            SqlExpression.Field("age"), SqlExpression.Number(25), CompareOperator.GreaterOrEqual));

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var count = 0;
        while (results.Read())
            count++;

        Assert.Equal(2, count);
    }

    [Fact]
    public void TestJoinCustomersAndOrders()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("amount"));
        query.FromClause.BaseTable = FromTerm.Table("customers", "c");
        var tOrders = FromTerm.Table("orders", "o");
        query.FromClause.Join(JoinType.Inner, query.FromClause.BaseTable, tOrders, "id", "customer_id");

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var count = 0;
        while (results.Read())
            count++;

        Assert.Equal(6, count);
    }

    [Fact]
    public void TestGroupByAndCount()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("city"));
        query.Columns.Add(new SelectColumn(SqlExpression.Raw("COUNT(*)"), "customer_count"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.GroupByTerms.Add(new GroupByTerm("city"));
        query.OrderByTerms.Add(new OrderByTerm("customer_count", OrderByDirection.Descending));

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var rows = new List<(string City, int Count)>();
        while (results.Read())
        {
            rows.Add((results.GetString(0), results.GetInt32(1)));
        }

        Assert.NotEmpty(rows);
        var newYorkCount = rows.FirstOrDefault(r => r.City == "New York").Count;
        Assert.Equal(2, newYorkCount);
    }

    [Fact]
    public void TestOrderBy()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("total_purchases"));
        query.FromClause.BaseTable = FromTerm.Table("customers");
        query.OrderByTerms.Add(new OrderByTerm("total_purchases", OrderByDirection.Descending));

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var rows = new List<(string Name, decimal Total)>();
        while (results.Read())
        {
            rows.Add((results.GetString(0), results.GetDecimal(1)));
        }

        Assert.Equal(3, rows.Count);
        Assert.True(rows[0].Total >= rows[1].Total);
        Assert.True(rows[1].Total >= rows[2].Total);
    }

    [Fact]
    public void TestComplexQueryWithJoinAndWhere()
    {
        var query = new SelectQuery();
        query.Columns.Add(new SelectColumn("name"));
        query.Columns.Add(new SelectColumn("amount"));
        query.Columns.Add(new SelectColumn("status"));
        query.FromClause.BaseTable = FromTerm.Table("customers", "c");
        var tOrders = FromTerm.Table("orders", "o");
        query.FromClause.Join(JoinType.Inner, query.FromClause.BaseTable, tOrders, "id", "customer_id");
        query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
            SqlExpression.Field("status", tOrders), 
            SqlExpression.String("completed"), 
            CompareOperator.Equal));
        query.OrderByTerms.Add(new OrderByTerm("amount", OrderByDirection.Descending));

        var sql = _renderer.RenderSelect(query);
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var results = cmd.ExecuteReader();

        var count = 0;
        while (results.Read())
        {
            var status = results.GetString(2);
            Assert.Equal("completed", status);
            count++;
        }

        Assert.Equal(5, count);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
