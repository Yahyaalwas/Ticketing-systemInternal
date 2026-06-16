namespace ITS.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string entityName, object key)
        : base($"{entityName} with key '{key}' has been modified by another user. Please refresh and try again.") { }
}
