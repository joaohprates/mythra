namespace Mythra.Domain.Common;

public readonly record struct Error(string Code, string Message)
{
    public static Error None => new(string.Empty, string.Empty);
    public static Error NotFound(string entity, object key) => new("not_found", $"{entity}:{key} not found");
    public static Error Validation(string message) => new("validation", message);
    public static Error Unauthorized(string message = "Unauthorized") => new("unauthorized", message);
    public static Error Forbidden(string message = "Forbidden") => new("forbidden", message);
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Internal(string message) => new("internal", message);
}

public readonly struct Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None) throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public Error Error { get; }
    public T? Value { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error);
}
