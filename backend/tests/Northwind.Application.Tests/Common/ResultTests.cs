using AwesomeAssertions;
using Northwind.Domain.Common;
using Xunit;

namespace Northwind.Application.Tests.Common;

/// <summary>
/// Tests covering the Result&lt;T, Error&gt; pattern that is the foundation of
/// our error-handling strategy. These tests both verify the type itself and
/// document the contract that the rest of the codebase will rely on.
/// </summary>
public class ResultTests
{
    // ------------------------------------------------------------------
    // Success scenarios
    // ------------------------------------------------------------------

    [Fact]
    public void Success_WithoutValue_ShouldHaveIsSuccessTrueAndNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Success_WithValue_ShouldExposeTheValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldProduceSuccessResult()
    {
        // This is the syntactic sugar that makes services read naturally:
        //   return order;   // instead of:   return Result.Success(order);
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    // ------------------------------------------------------------------
    // Failure scenarios
    // ------------------------------------------------------------------

    [Fact]
    public void Failure_ShouldHaveIsFailureTrueAndCarryTheError()
    {
        var error = Error.NotFound("Order.NotFound", "Order 1 was not found.");

        var result = Result.Failure<int>(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldProduceFailureResult()
    {
        var error = Error.Validation("Order.NoLines", "Order must have at least one line.");

        // Same sugar as above, but for the error path:
        //   return error;   // instead of:   return Result.Failure<Order>(error);
        Result<int> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void AccessingValue_OnFailure_ShouldThrow()
    {
        // If a caller forgot to check IsSuccess, we'd rather fail loudly
        // than silently return default(T) and let garbage propagate.
        var result = Result.Failure<int>(Error.NotFound("X", "x"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    // ------------------------------------------------------------------
    // Invariant scenarios (programmer-bug protection)
    // ------------------------------------------------------------------

    [Fact]
    public void Failure_WithNoneError_ShouldThrow()
    {
        // A failed result must carry a real error. Constructing one with Error.None
        // is a programmer bug, so we throw at construction time.
        var act = () => Result.Failure<int>(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }
}