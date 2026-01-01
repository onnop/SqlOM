namespace Reeb.SqlOM;

/// <summary>
/// InvalidQueryException exception can be thrown when the renderer decides that a query is invalid or incompatible with the target database.
/// </summary>
public class InvalidQueryException : Exception
{
    /// <summary>
    /// Creates a new InvalidQueryException
    /// </summary>
    /// <param name="text">Text of the exception</param>
    public InvalidQueryException(string text) : base(text) { }

    /// <summary>
    /// Creates a new InvalidQueryException
    /// </summary>
    /// <param name="text">Text of the exception</param>
    /// <param name="innerException">The inner exception</param>
    public InvalidQueryException(string text, Exception innerException) : base(text, innerException) { }
}
