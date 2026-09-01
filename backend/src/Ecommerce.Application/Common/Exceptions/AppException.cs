namespace Ecommerce.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}

public class UnauthorizedAppException(string message)
    : AppException("UNAUTHORIZED", message, 401);

public class ValidationAppException(string message)
    : AppException("VALIDATION_ERROR", message, 400);

public class NotFoundAppException(string message)
    : AppException("NOT_FOUND", message, 404);

public class ConflictAppException(string message)
    : AppException("CONFLICT", message, 409);
