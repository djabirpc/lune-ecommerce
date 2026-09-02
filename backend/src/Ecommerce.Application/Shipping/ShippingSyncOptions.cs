namespace Ecommerce.Application.Shipping;

public class ShippingSyncOptions
{
    public const string SectionName = "ShippingSync";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;
}
