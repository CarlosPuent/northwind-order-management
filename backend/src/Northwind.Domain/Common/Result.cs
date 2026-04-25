namespace Northwind.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can either succeed or fail with an Error.
/// Use this as the return type for application services and domain operations
/// where failure is an expected outcome (validation, not found, business rule, etc.)
/// rather than something exceptional (DB unreachable, OOM, programmer bug).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Invariants:
        //   Success ⇒ no error attached.
        //   Failure ⇒ a real error attached.
        // Violating these is a programmer bug, so we throw — it's not a domain failure.
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    // ---- Factory methods (no value) ----
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    // ---- Factory methods (with value) ----
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default!, false, error);
}

/// <summary>
/// A Result that carries a value on success.
/// Access the value through <see cref="Value"/>; it throws if the result is a failure,
/// which forces the caller to check IsSuccess / IsFailure first.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T _value;

    internal Result(T value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The success value. Throws if the result is a failure — this prevents
    /// callers from accidentally consuming garbage data without checking the result first.
    /// </summary>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    /// <summary>
    /// Implicit conversion so a service can simply `return order;` instead of
    /// `return Result.Success(order);`. Reads cleaner at the call site.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion so a service can simply `return error;` instead of
    /// `return Result.Failure&lt;Order&gt;(error);`.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}