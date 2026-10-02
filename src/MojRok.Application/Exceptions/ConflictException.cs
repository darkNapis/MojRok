namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when an operation cannot be completed because of a
/// conflicting state (e.g. deleting a category that still has
/// Deadlines assigned to it).
/// Maps to HTTP 409 in the API layer.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
