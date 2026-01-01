namespace Reeb.SqlOM
{
    /// <summary>
    /// Data type of a constant value
    /// </summary>
    public enum SqlDataType
    {
        /// <summary>Boolean value</summary>
        Boolean,
        /// <summary>String value</summary>
        String,
        /// <summary>Numeric value (int, double, float, decimal, long)</summary>
        Number,
        /// <summary>DateTime object</summary>
        Date,
        /// <summary>Binary object, like byte[]</summary>
        Binary,
        Guid
    }

    /// <summary>
    /// Represents a typed constant value.
    /// </summary>
    public class SqlConstant
    {
        SqlDataType type;
        object val;

        /// <summary>
        /// Creates a new SqlConstant instance
        /// </summary>
        /// <param name="type">Constant's date type</param>
        /// <param name="val">Constant's value</param>
        public SqlConstant(SqlDataType type, object val)
        {
            this.type = type;
            this.val = val;
        }

        /// <summary>
        /// Creates a SqlConstant which represents a numeric value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a floating point value</returns>
        public static SqlConstant Number(double val)
        {
            return new SqlConstant(SqlDataType.Number, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a numeric value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a decimal value</returns>
        public static SqlConstant Number(decimal val)
        {
            return new SqlConstant(SqlDataType.Number, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a numeric value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a numeric value</returns>
        public static SqlConstant Number(int val)
        {
            return new SqlConstant(SqlDataType.Number, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a numeric value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a numeric value</returns>
        public static SqlConstant Number(long val)
        {
            return new SqlConstant(SqlDataType.Number, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a textual value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a textual value</returns>
        public static SqlConstant String(string val)
        {
            return new SqlConstant(SqlDataType.String, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a boolean value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a boolean value</returns>
        public static SqlConstant Boolean(bool val)
        {
            return new SqlConstant(SqlDataType.Boolean, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a binary value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a binary value</returns>
        public static SqlConstant Binary(byte[] val)
        {
            return new SqlConstant(SqlDataType.Binary, val);
        }

        public static SqlConstant Guid(Guid val)
        {
            return new SqlConstant(SqlDataType.Guid, val);
        }

        /// <summary>
        /// Creates a SqlConstant which represents a date value.
        /// </summary>
        /// <param name="val">Value of the expression</param>
        /// <returns>A SqlConstant which represents a date value</returns>
        public static SqlConstant Date(DateTime val)
        {
            return new SqlConstant(SqlDataType.Date, val);
        }

        internal SqlDataType Type
        {
            get { return this.type; }
        }

        internal object Value
        {
            get { return this.val; }
        }
    }
}
