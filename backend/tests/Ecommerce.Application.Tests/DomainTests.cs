using Ecommerce.Domain.Common;
using Ecommerce.Domain.Identity;

namespace Ecommerce.Application.Tests;

public class EntityTests
{
    private class TestEntity : Entity;

    [Fact]
    public void NewEntity_HasUniqueIdAndCreationTimestamp()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.True(entity.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Null(entity.UpdatedAtUtc);
    }

    [Fact]
    public void TwoNewEntities_HaveDifferentIds()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.NotEqual(first.Id, second.Id);
    }
}

public class RolesTests
{
    [Fact]
    public void All_ContainsEverySpecifiedRole()
    {
        Assert.Equal(
        [
            Roles.SuperAdmin,
            Roles.Admin,
            Roles.OrderManager,
            Roles.ConfirmationAgent,
            Roles.StockManager,
            Roles.MarketingManager,
            Roles.Viewer
        ], Roles.All);
    }
}
