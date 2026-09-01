using Ecommerce.Application.Auth;
using Ecommerce.Application.Catalog;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Promotions;
using Ecommerce.Infrastructure.Catalog;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Inventory;
using Ecommerce.Infrastructure.Orders;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Promotions;
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
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderCallAttemptService, OrderCallAttemptService>();
        services.AddScoped<IPromotionService, PromotionService>();

        return services;
    }
}
