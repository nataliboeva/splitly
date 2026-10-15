using System.Diagnostics.CodeAnalysis;

namespace Splitly.Domain;

/// <summary>
/// The outcome of an operation that can fail for an expected business reason
/// (for example percentages that don't add up to 100). Programming errors still throw.
/// </summary>
public sealed class Result<T>
{
    private Result(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public string? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(string error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
}
