# SqlOM

[![NuGet](https://img.shields.io/nuget/v/SqlOM.svg)](https://www.nuget.org/packages/SqlOM)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-blue.svg)](https://dotnet.microsoft.com/download)

**Generate dynamic SQL queries using C# code only**

> **💡 For AI Tools (Cursor AI, GitHub Copilot, etc.):** This README contains comprehensive examples and documentation for using SqlOM. When coding with SqlOM, refer to this README file for complete API usage, examples, and best practices. All examples progress from simple to complex queries.

SqlOM is a software component which allows you to programmatically create SQL queries at runtime using a convenient .NET object model, thus creating an abstraction layer over SQL. Dynamic SQL generation is useful in several scenarios:

* Generate SQL dynamically when query structure is not known at development time (i.e. user defined reports or filters)
* Generate SQL dynamically when database structure is not known at development time (i.e. user defined tables or fields)
* Create a database independent data layer

SqlOM automates the process of SQL generation in a dynamic, convenient, time saving, database independent way.

## Installation

### Package Manager
```powershell
Install-Package SqlOM
```

### .NET CLI
```bash
dotnet add package SqlOM
```

### PackageReference
```xml
<PackageReference Include="SqlOM" Version="1.0.6" />
```

## Supported Databases

Currently the following databases are supported. We continuously add support for additional databases. If your database is not on the list, contact us or tweak the source code on your own to add the desired functionality.

* SQL Server
* Oracle
* MySQL
* MariaDB
* SQLite

## Features

- ✅ Dynamic SQL query generation
- ✅ Database-agnostic query building
- ✅ Support for complex WHERE conditions
- ✅ Multiple JOIN types (INNER, LEFT, RIGHT, FULL, CROSS)
- ✅ CASE expressions
- ✅ UNION queries
- ✅ Paging support
- ✅ Parameterized queries
- ✅ Cross-Tabs (Pivot Tables)
- ✅ Attribute-based strongly-typed query building
- ✅ Modern C# 12 syntax support
- ✅ Multi-targets .NET 8.0 and .NET 9.0

## Examples (Simple to Complex)

All examples use the following namespaces:
```csharp
using Reeb.SqlOM;
using Reeb.SqlOM.Render;
```

### 1. Simple SELECT Query (Single Table)

The most basic query - selecting columns from a single table:

```csharp
SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("email"));
query.FromClause.BaseTable = FromTerm.Table("customers");

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [name], [email] FROM [customers]
```

### 2. SELECT with WHERE Clause

Adding a simple WHERE condition:

```csharp
FromTerm tCustomers = FromTerm.Table("customers", "c");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("email", tCustomers));
query.FromClause.BaseTable = tCustomers;

query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("name", tCustomers), 
    SqlExpression.String("John"), 
    CompareOperator.Equal));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [c].[name], [c].[email] FROM [customers] [c] WHERE [c].[name] = 'John'
```

### 3. SELECT with ORDER BY

Adding sorting to your query:

```csharp
SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("createdDate"));
query.FromClause.BaseTable = FromTerm.Table("customers");

query.OrderByTerms.Add(new OrderByTerm("name", OrderByDirection.Ascending));
query.OrderByTerms.Add(new OrderByTerm("createdDate", OrderByDirection.Descending));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [name], [createdDate] FROM [customers] ORDER BY [name] asc, [createdDate] desc
```

### 4. SELECT with Multiple WHERE Conditions

Using AND/OR logic in WHERE clauses:

```csharp
FromTerm tProducts = FromTerm.Table("products", "p");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tProducts));
query.Columns.Add(new SelectColumn("price", tProducts));
query.FromClause.BaseTable = tProducts;

// AND condition (default)
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("price", tProducts), 
    SqlExpression.Number(10), 
    CompareOperator.Greater));

query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("price", tProducts), 
    SqlExpression.Number(100), 
    CompareOperator.Less));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [p].[name], [p].[price] FROM [products] [p] WHERE ([p].[price] > 10 AND [p].[price] < 100)
```

### 5. Rendering Queries

After building your query, render it to SQL for your specific database:

```csharp
SelectQuery query = new SelectQuery();
// ... configure query ...

// For SQL Server
string sql = new SqlServerRenderer().RenderSelect(query);

// For MySQL
string sql = new MySqlRenderer().RenderSelect(query);

// For Oracle
string sql = new OracleRenderer().RenderSelect(query);

// For SQLite
string sql = new SQLiteRenderer().RenderSelect(query);
```

### 6. Simple JOIN (Two Tables)

Joining two tables with a simple key relationship:

```csharp
FromTerm tCustomers = FromTerm.Table("customers", "c");
FromTerm tOrders = FromTerm.Table("orders", "o");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("orderDate", tOrders));
query.Columns.Add(new SelectColumn("total", tOrders));

query.FromClause.BaseTable = tCustomers;
query.FromClause.Join(JoinType.Inner, tCustomers, tOrders, "customerId", "customerId");

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [c].[name], [o].[orderDate], [o].[total] FROM [customers] [c] INNER JOIN [orders] [o] ON [c].[customerId] = [o].[customerId]
```

### 7. Multiple JOINs (Three or More Tables)

Joining multiple tables:

```csharp
FromTerm tCustomers = FromTerm.Table("customers", "c");
FromTerm tOrders = FromTerm.Table("orders", "o");
FromTerm tOrderItems = FromTerm.Table("orderItems", "oi");
FromTerm tProducts = FromTerm.Table("products", "p");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("orderDate", tOrders));
query.Columns.Add(new SelectColumn("productName", tProducts));
query.Columns.Add(new SelectColumn("quantity", tOrderItems));

query.FromClause.BaseTable = tCustomers;
query.FromClause.Join(JoinType.Left, tCustomers, tOrders, "customerId", "customerId");
query.FromClause.Join(JoinType.Inner, tOrders, tOrderItems, "orderId", "orderId");
query.FromClause.Join(JoinType.Inner, tOrderItems, tProducts, "productId", "productId");

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 8. Complex WHERE Conditions

Using various comparison operators and logical groupings:

```csharp
FromTerm tProducts = FromTerm.Table("products", "p");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tProducts));
query.Columns.Add(new SelectColumn("price", tProducts));
query.FromClause.BaseTable = tProducts;

// Simple comparison
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("name", tProducts), 
    SqlExpression.String("John"), 
    CompareOperator.Equal));

// BETWEEN clause
query.WherePhrase.Terms.Add(WhereTerm.CreateBetween(
    SqlExpression.Field("price", tProducts), 
    SqlExpression.Number(1), 
    SqlExpression.Number(100)));

// IN clause
query.WherePhrase.Terms.Add(WhereTerm.CreateIn(
    SqlExpression.Field("category", tProducts), 
    SqlConstantCollection.FromList(new string[] {"Electronics", "Books", "Clothing"})));

// IS NULL
query.WherePhrase.Terms.Add(WhereTerm.CreateIsNull(SqlExpression.Field("deletedDate", tProducts)));

// OR group
WhereClause orGroup = new WhereClause(WhereClauseRelationship.Or);
orGroup.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("status", tProducts), 
    SqlExpression.String("Active"), 
    CompareOperator.Equal));
orGroup.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("status", tProducts), 
    SqlExpression.String("Pending"), 
    CompareOperator.Equal));
query.WherePhrase.SubClauses.Add(orGroup);

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 9. Complex JOINs with Multiple Conditions

Joining tables with multiple join conditions:

```csharp
FromTerm tOrders = FromTerm.Table("orders", "o");
FromTerm tOrderItems = FromTerm.Table("orderItems", "oi");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("orderId", tOrders));
query.Columns.Add(new SelectColumn("itemId", tOrderItems));

query.FromClause.BaseTable = tOrders;

// Join with multiple conditions
query.FromClause.Join(JoinType.Left, tOrders, tOrderItems, 
    new JoinCondition("orderId", "orderId"),
    new JoinCondition("customerId", "customerId"));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 10. JOINs with Custom WHERE Conditions

Using WHERE clauses for complex join conditions:

```csharp
FromTerm tOrders = FromTerm.Table("orders", "o");
FromTerm tProducts = FromTerm.Table("products", "p");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("orderId", tOrders));
query.Columns.Add(new SelectColumn("productName", tProducts));

query.FromClause.BaseTable = tOrders;

// Complex join condition using WHERE clause
WhereClause joinCondition = new WhereClause(WhereClauseRelationship.Or);
joinCondition.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("productId", tOrders), 
    SqlExpression.Field("productId", tProducts), 
    CompareOperator.Equal));
joinCondition.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("productName", tOrders), 
    SqlExpression.Field("name", tProducts), 
    CompareOperator.Equal));

query.FromClause.Join(JoinType.Left, tOrders, tProducts, joinCondition);

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 11. Aggregations and GROUP BY

Using aggregate functions and grouping:

```csharp
FromTerm tOrders = FromTerm.Table("orders", "o");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("customerId", tOrders));
query.Columns.Add(new SelectColumn(
    SqlExpression.Function(SqlAggregationFunction.Sum, SqlExpression.Field("total", tOrders)), 
    "totalAmount"));
query.Columns.Add(new SelectColumn(
    SqlExpression.Function(SqlAggregationFunction.Count, SqlExpression.Field("orderId", tOrders)), 
    "orderCount"));

query.FromClause.BaseTable = tOrders;

query.GroupByTerms.Add(new GroupByTerm("customerId", tOrders));

query.OrderByTerms.Add(new OrderByTerm("totalAmount", OrderByDirection.Descending));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [o].[customerId], SUM([o].[total]) [totalAmount], COUNT([o].[orderId]) [orderCount] 
//         FROM [orders] [o] GROUP BY [o].[customerId] ORDER BY [totalAmount] desc
```

### 12. Strongly-Typed Query Building with Attributes

Use attributes to define table and column mappings, then build queries without magic strings:

#### Define Row Types with Attributes

```csharp
using Reeb.SqlOM;

[TableName("customers"), TableAlias("c")]
public class CustomerRow
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    [IgnoreColumn] public List<OrderRow> Orders { get; set; } // Navigation property, not a column
}

[TableName("orders"), TableAlias("o")]
public class OrderRow
{
    public string Id { get; set; }
    [ColumnName("customer_id")] public string CustomerId { get; set; } // Maps to different column name
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}
```

#### Build Queries Using Row Types

```csharp
using Reeb.SqlOM;
using static Reeb.SqlOM.SqlOMExtensions;

// Create table references from attributes
var tCustomers = Table<CustomerRow>();  // FromTerm.Table("customers", "c")
var tOrders = Table<OrderRow>();        // FromTerm.Table("orders", "o")

SelectQuery query = new SelectQuery();

// Add all columns from a type (excludes [IgnoreColumn] properties)
query.Columns.AddAllColumns<CustomerRow>(tCustomers);

// Add specific columns with auto-aliasing
// CustomerId has [ColumnName("customer_id")], so this generates: customer_id AS CustomerId
query.Columns.Add<OrderRow>(x => x.CustomerId, tOrders);
query.Columns.Add<OrderRow>(x => x.Total, tOrders);

query.FromClause.BaseTable = tCustomers;
query.FromClause.Join(JoinType.Left, tCustomers, tOrders, 
    nameof(CustomerRow.Id), 
    ColumnName<OrderRow>(x => x.CustomerId));  // Returns "customer_id"

// Use strongly-typed field expressions in WHERE
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    Field<OrderRow>(x => x.Total, tOrders),
    SqlExpression.Number(100),
    CompareOperator.Greater));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

#### Quick Query Generation

Generate a complete SELECT query from a row type in one line:

```csharp
// Generates: SELECT Id, Name, Email FROM customers c
SelectQuery query = GenerateSelectQuery<CustomerRow>();

// With custom alias
SelectQuery query = GenerateSelectQuery<CustomerRow>("cust");
```

#### Automatic Alias Generation

All query constructors (`SelectQuery`, `UpdateQuery`, `DeleteQuery`, `InsertQuery`, `BulkInsertQuery`) automatically reset alias tracking. `Table<T>()` then auto-generates unique aliases:

```csharp
var query = new SelectQuery();  // resets aliases automatically
var tPhase = Table<Phase>();           // alias: "p"
var tSubphase = Table<Subphase>();     // alias: "s"
var tWorkPackage = Table<WorkPackage>(); // alias: "w"
var tActivity = Table<Activity>();     // alias: "a"
var tStatus = Table<ActivityExecutionStatus>(); // alias: "a2" (auto-incremented)

// Override if needed
var tCustom = Table<Phase>("custom");  // explicit alias
```

#### Available Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[TableName("name")]` | Class | Specifies the database table name |
| `[TableAlias("alias")]` | Class | Specifies the default alias in queries |
| `[ColumnName("name")]` | Property | Maps property to a different column name |
| `[IgnoreColumn]` | Property | Excludes property from `AddAllColumns<T>()` |

#### Helper Methods

| Method | Description |
|--------|-------------|
| `Table<T>()` | Creates `FromTerm` with auto-generated unique alias |
| `Table<T>(alias)` | Creates `FromTerm` with explicit alias (overrides auto-generation) |
| `TableName<T>()` | Gets table name from attribute or auto-pluralized type name |
| `TableAlias<T>()` | Gets base alias from attribute or first letter |
| `Pluralize(word)` | Pluralizes English nouns (Activity → Activities) |
| `ColumnName<T>(x => x.Prop)` | Gets column name from `[ColumnName]` or property name |
| `Field<T>(x => x.Prop, table)` | Creates `SqlExpression.Field` with correct column name |
| `columns.Add<T>(x => x.Prop, table)` | Adds column with auto-aliasing |
| `columns.AddAllColumns<T>(table)` | Adds columns, auto-skips navigation/[NotMapped] |
| `GenerateSelectQuery<T>()` | Creates query with all columns from type |

### 13. CASE Expressions (Advanced)

Using CASE statements in SELECT columns:

```csharp
FromTerm tProducts = FromTerm.Table("products", "p");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tProducts));
query.Columns.Add(new SelectColumn("price", tProducts));

// Create CASE expression
CaseClause caseClause = new CaseClause();
caseClause.Terms.Add(new CaseTerm(
    WhereTerm.CreateCompare(SqlExpression.Field("price", tProducts), SqlExpression.Number(100), CompareOperator.Less),
    SqlExpression.String("Budget")));
caseClause.Terms.Add(new CaseTerm(
    WhereTerm.CreateCompare(SqlExpression.Field("price", tProducts), SqlExpression.Number(500), CompareOperator.Less),
    SqlExpression.String("Mid-Range")));
caseClause.ElseValue = SqlExpression.String("Premium");

query.Columns.Add(new SelectColumn(SqlExpression.Case(caseClause), "priceCategory"));

query.FromClause.BaseTable = tProducts;

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 14. UNION Queries

Combining multiple SELECT queries:

```csharp
SqlUnion union = new SqlUnion();

// First query
SelectQuery query1 = new SelectQuery();
query1.Columns.Add(new SelectColumn(SqlExpression.Raw("price * 10"), "priceX10"));
query1.FromClause.BaseTable = FromTerm.Table("products");
union.Add(query1);

// Second query
SelectQuery query2 = new SelectQuery();
query2.Columns.Add(new SelectColumn(SqlExpression.Field("price"), "priceX10"));
query2.FromClause.BaseTable = FromTerm.Table("products");
union.Add(query2, DistinctModifier.All);

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderUnion(union);
// Result: SELECT price * 10 [priceX10] FROM [products] UNION ALL SELECT [price] [priceX10] FROM [products]
```

### 15. Parameterized Queries

Using parameters for better performance and security:

```csharp
FromTerm tCustomers = FromTerm.Table("customers");

SelectQuery query = new SelectQuery();
query.TableSpace = "MyDatabase.dbo"; // For execution plan caching
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("email", tCustomers));
query.FromClause.BaseTable = tCustomers;

// Use parameters instead of literal values
query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Parameter("@customerName"), 
    SqlExpression.Field("name", tCustomers), 
    CompareOperator.Equal));

query.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("age", tCustomers), 
    SqlExpression.Parameter("@minAge"), 
    CompareOperator.GreaterOrEqual));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);

// Use with SqlCommand
SqlCommand command = new SqlCommand(sql, connection);
command.Parameters.Add("@customerName", SqlDbType.NVarChar).Value = "John";
command.Parameters.Add("@minAge", SqlDbType.Int).Value = 18;
```

### 16. Paging

Implementing pagination for large result sets:

```csharp
SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name"));
query.Columns.Add(new SelectColumn("email"));
query.FromClause.BaseTable = FromTerm.Table("customers");

// ORDER BY is required for paging
query.OrderByTerms.Add(new OrderByTerm("name", OrderByDirection.Ascending));

SqlServerRenderer renderer = new SqlServerRenderer();

// First, get total row count
string rowCountSql = renderer.RenderRowCount(query);
int totalRows = (int)ExecuteScalar(rowCountSql);

// Then get paged results
int pageIndex = 0; // Zero-based
int pageSize = 10;
string sql = renderer.RenderPage(pageIndex, pageSize, totalRows, query);
```

### 17. Subqueries

Using subqueries in WHERE clauses:

```csharp
FromTerm tCustomers = FromTerm.Table("customers", "c");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("email", tCustomers));
query.FromClause.BaseTable = tCustomers;

// IN subquery
SelectQuery subQuery = new SelectQuery();
subQuery.Columns.Add(new SelectColumn("customerId"));
subQuery.FromClause.BaseTable = FromTerm.Table("orders");
subQuery.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("orderDate"), 
    SqlExpression.Date(DateTime.Now.AddDays(-30)), 
    CompareOperator.GreaterOrEqual));

query.WherePhrase.Terms.Add(WhereTerm.CreateIn(
    SqlExpression.Field("customerId", tCustomers), 
    subQuery));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
```

### 18. EXISTS and NOT EXISTS

Using EXISTS clauses:

```csharp
FromTerm tCustomers = FromTerm.Table("customers", "c");
FromTerm tOrders = FromTerm.Table("orders", "o");

SelectQuery query = new SelectQuery();
query.Columns.Add(new SelectColumn("name", tCustomers));
query.Columns.Add(new SelectColumn("email", tCustomers));
query.FromClause.BaseTable = tCustomers;

// EXISTS subquery
SelectQuery existsQuery = new SelectQuery();
existsQuery.Columns.Add(new SelectColumn("*"));
existsQuery.FromClause.BaseTable = tOrders;
existsQuery.WherePhrase.Terms.Add(WhereTerm.CreateCompare(
    SqlExpression.Field("customerId", tOrders), 
    SqlExpression.Field("customerId", tCustomers), 
    CompareOperator.Equal));

query.WherePhrase.Terms.Add(WhereTerm.CreateExists(existsQuery));

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(query);
// Result: SELECT [c].[name], [c].[email] FROM [customers] [c] 
//         WHERE EXISTS (SELECT * FROM [orders] [o] WHERE [o].[customerId] = [c].[customerId])
```

### 19. Cross-Tabs (Pivot Tables) - Advanced

Creating dynamic pivot tables for reporting:

```csharp
PivotTable pivot = new PivotTable();
pivot.BaseSql = "select * from orders";
pivot.Function = SqlAggregationFunction.Sum;
pivot.ValueField = "quantity";
pivot.RowField = "customerId";

// Date-based pivot column
PivotColumn pivotCol = new PivotColumn("date", SqlDataType.Date);
TimePeriod currentYear = TimePeriod.FromToday(TimePeriodType.Year);
pivotCol.Values.Add(PivotColumnValue.CreateRange("before2023", 
    new Range(null, currentYear.Add(-1).PeriodStartDate)));
pivotCol.Values.Add(PivotColumnValue.CreateRange("y2023", 
    new Range(currentYear.Add(-1).PeriodStartDate, currentYear.PeriodStartDate)));
pivotCol.Values.Add(PivotColumnValue.CreateRange("after2023", 
    new Range(currentYear.PeriodStartDate, null)));
pivot.Columns.Add(pivotCol);

// Product-based pivot column
pivotCol = new PivotColumn("productId", SqlDataType.Number);
pivotCol.Values.Add(PivotColumnValue.CreateScalar("product1", 1));
pivotCol.Values.Add(PivotColumnValue.CreateScalar("product2", 2));
pivot.Columns.Add(pivotCol);

SelectQuery pivotQuery = pivot.BuildPivotSql();

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(pivotQuery);
```

### 20. Cross-Tab Drill-Down - Advanced

Drilling down into pivot table cells:

```csharp
// Using the same pivot instance from example 18
SelectQuery drillDownQuery = pivot.BuildDrillDownSql(
    SqlConstant.Number(1), // customerId = 1
    "y2023"                // column name
);

SqlServerRenderer renderer = new SqlServerRenderer();
string sql = renderer.RenderSelect(drillDownQuery);
```

## Requirements

- .NET 8.0 or later (.NET 8.0 and .NET 9.0 are supported)

## License

See [License.txt](License.txt) for license information.

## Contributing

Contributions are welcome! If you find a bug or have a feature request, please open an issue on the project repository.

## Version History

### 1.0.6
- All query constructors automatically reset alias tracking
- `Table<T>()` auto-generates unique aliases (a, a2, a3... on collision)
- `Table<T>("custom")` overload to override with explicit alias

### 1.0.4
- `TableName<T>()` now auto-pluralizes type names (Activity → Activities)
- `AddAllColumns<T>()` auto-skips navigation properties and `[NotMapped]`
- New `Pluralize()` helper for common English noun pluralization
- No need to add `[IgnoreColumn]` to navigation properties

### 1.0.3
- Attribute-based strongly-typed query building
- New attributes: `[TableName]`, `[TableAlias]`, `[ColumnName]`, `[IgnoreColumn]`
- Helper methods: `Table<T>()`, `Field<T>()`, `ColumnName<T>()`, `AddAllColumns<T>()`
- Auto-aliasing when `[ColumnName]` differs from property name
- `GenerateSelectQuery<T>()` for quick query scaffolding

### 1.0.2
- Enhanced documentation with comprehensive examples
- Examples organized from simple to complex
- Added AI tool instruction note
- Improved README structure and clarity

### 1.0.1
- Initial release, originated from a former SourceForge project
- Modernized codebase with C# 12 features
- Multi-targets .NET 8.0 and .NET 9.0
- Improved nullable reference type support
- Enhanced code quality and performance
- File-scoped namespaces throughout
- Modern switch expressions
- Improved string handling and StringBuilder usage
- Better immutability with readonly modifiers
