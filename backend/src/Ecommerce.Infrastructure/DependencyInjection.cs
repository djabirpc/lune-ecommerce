using Ecommerce.Application.Auth;
using Ecommerce.Application.Catalog;
using Ecommerce.Application.Common;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Marketing;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Promotions;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Suppliers;
using Ecommerce.Application.Users;
using Ecommerce.Infrastructure.Catalog;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Inventory;
using Ecommerce.Infrastructure.Marketing;
using Ecommerce.Infrastructure.Orders;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Promotions;
using Ecommerce.Infrastructure.Shipping;
using Ecommerce.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderCallAttemptService, OrderCallAttemptService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IMarketingService, MarketingService>();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.Configure<YalidineOptions>(configuration.GetSection(YalidineOptions.SectionName));
        services.Configure<ZRExpressOptions>(configuration.GetSection(ZRExpressOptions.SectionName));
        services.Configure<ShippingSyncOptions>(configuration.GetSection(ShippingSyncOptions.SectionName));

        services.AddSingleton<IShippingProvider, FakeShippingProvider>();
        services.AddScoped<IShippingProvider, YalidineShippingProvider>();
        services.AddScoped<IShippingProvider, ZRExpressShippingProvider>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IShippingRateService, ShippingRateService>();
        services.AddHostedService<ShippingSyncBackgroundService>();

        return services;
    }
}
