namespace Reeb.SqlOM
{
    public static class SqlOMExtensions
    {
        public static SqlConstantCollection ToSqlConstantCollection(this List<Guid> values)
        {
            var collection = new SqlConstantCollection();
            foreach (var value in values)
            {
                collection.Add(SqlConstant.Guid(value));
            }

            return collection;
        }

    }
}
