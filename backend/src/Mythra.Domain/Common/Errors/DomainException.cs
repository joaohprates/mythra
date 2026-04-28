namespace Mythra.Domain.Common.Errors;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    public DomainException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}

public sealed class InvariantViolationException : DomainException
{
    public InvariantViolationException(string message) : base("invariant.violation", message) { }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base("entity.not_found", $"{entity} with key '{key}' was not found.") { }
}
