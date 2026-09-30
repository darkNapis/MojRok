namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails (wrong credentials, inactive account).
/// Maps to HTTP 401 in the API layer.
/// </summary>
public sealed class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}
