namespace Ecommerce.Domain.Identity;

public static class Roles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string Admin = "ADMIN";
    public const string OrderManager = "ORDER_MANAGER";
    public const string ConfirmationAgent = "CONFIRMATION_AGENT";
    public const string StockManager = "STOCK_MANAGER";
    public const string MarketingManager = "MARKETING_MANAGER";
    public const string Viewer = "VIEWER";

    public static readonly string[] All =
    [
        SuperAdmin,
        Admin,
        OrderManager,
        ConfirmationAgent,
        StockManager,
        MarketingManager,
        Viewer
    ];

    /// <summary>Roles allowed to manage the product/inventory catalog, as a comma-separated list for [Authorize(Roles = ...)].</summary>
    public const string CatalogManagers = $"{SuperAdmin},{Admin},{StockManager}";

    /// <summary>Roles allowed to view/manage orders, as a comma-separated list for [Authorize(Roles = ...)].</summary>
    public const string OrderManagers = $"{SuperAdmin},{Admin},{OrderManager},{ConfirmationAgent}";

    /// <summary>Roles allowed to manage promotions, as a comma-separated list for [Authorize(Roles = ...)].</summary>
    public const string PromotionManagers = $"{SuperAdmin},{Admin},{MarketingManager}";

    /// <summary>Roles allowed to view marketing attribution/reporting, as a comma-separated list for [Authorize(Roles = ...)].</summary>
    public const string MarketingManagers = $"{SuperAdmin},{Admin},{MarketingManager}";

    /// <summary>Roles allowed to manage staff accounts (create/edit/assign roles), as a comma-separated list for [Authorize(Roles = ...)].</summary>
    public const string UserManagers = $"{SuperAdmin},{Admin}";
}
