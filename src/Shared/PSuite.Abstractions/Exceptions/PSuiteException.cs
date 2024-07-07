namespace PSuite.Shared.Abstractions.Exceptions;
public abstract class PSuiteException(string message, ExceptionCategory exceptionCategory) : Exception(message)
{
    public ExceptionCategory Category { get; } = exceptionCategory;
}

public enum ExceptionCategory
{
    ValidationError,
    NotFound,
    AlreadyExists
}