namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when the current user attempts an operation they are not
/// permitted to perform (e.g. modifying another user's category,
/// or editing a system default category).
/// Maps to HTTP 403 in the API layer.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
