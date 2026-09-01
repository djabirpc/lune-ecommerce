namespace Ecommerce.Application.Common;

public record ErrorResponse(bool Success, ErrorDetail Error)
{
    public static ErrorResponse Create(string code, string message) => new(false, new ErrorDetail(code, message));
}

public record ErrorDetail(string Code, string Message);
