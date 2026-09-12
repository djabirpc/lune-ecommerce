namespace Ecommerce.Application.Marketing.Dtos;

public record HomeBannerDto(
    Guid Id,
    string ImageUrl,
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive);

public record UpdateHomeBannerRequest(
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive);
