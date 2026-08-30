namespace WAMS.Domain.Exceptions;

public class ForbiddenException(string message = "Forbidden") : AppException(message, 403)
{
}
