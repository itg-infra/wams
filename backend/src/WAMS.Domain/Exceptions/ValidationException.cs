namespace WAMS.Domain.Exceptions;

public class ValidationException : AppException
{
    private const string DefaultCode = "VALIDATION_ERROR";

    public IDictionary<string, string[]> Errors { get; }
    public string Code { get; }
    public object? Details { get; }

    public ValidationException(string message)
        : this(message, DefaultCode, new Dictionary<string, string[]>()) { }

    public ValidationException(IDictionary<string, string[]> errors)
        : this("One or more validation errors occurred.", DefaultCode, errors) { }

    public ValidationException(string message, string code, object? details = null)
        : base(message, 422)
    {
        Code = code;
        Details = details;
        Errors = details as IDictionary<string, string[]> ?? new Dictionary<string, string[]>();
    }
}
