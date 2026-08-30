namespace WAMS.Domain.Exceptions;

public class SessionIdleTimeoutException(string message = "Session expired due to inactivity") : AppException(message, 401)
{
}
