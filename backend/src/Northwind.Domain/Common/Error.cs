namespace Northwind.Domain.Common;

/// <summary>
/// Represents a domain or application error in a structured, predictable way.
/// Use this instead of throwing exceptions for expected failure modes such as
/// validation failures, missing entities, or business rule violations.
/// </summary>
/// <param name="Code">A short, machine-readable code (e.g. "Order.NotFound").
/// Useful for clients to switch on without parsing messages.</param>
/// <param name="Message">A human-readable description of what went wrong.</param>
/// <param name="Type">The category of error, used by the API layer to choose
/// the appropriate HTTP status code (404, 422, 409, 500, ...).</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Sentinel value representing the absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    // -----------------------------------------------------------------------
    // Factory methods — these read like English at the call site:
    //     return Error.NotFound("Order.NotFound", "Order 123 was not found.");
    //     return Error.Validation("Order.NoLines", "Order must have at least one line.");
    // -----------------------------------------------------------------------

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);
}

/// <summary>
/// Categories of error. The API layer maps each one to an HTTP status code:
/// NotFound → 404, Validation → 422, Conflict → 409, Unauthorized → 401, Failure → 500.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4
}