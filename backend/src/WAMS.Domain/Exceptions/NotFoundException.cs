namespace WAMS.Domain.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key) : base($"{entity} with identifier {key} was not found", 404) { }

    public NotFoundException(string message) : base(message, 404) { }
}