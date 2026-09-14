using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class SellerEntityTests
{
    [Fact]
    public void Constructor_AddsOwner_WithZeroRating()
    {
        var ownerId = Guid.NewGuid();

        var seller = new Seller("TechStore", ownerId);

        var owner = Assert.Single(seller.Members);
        Assert.Equal(ownerId, owner.UserAccountId);
        Assert.Equal(SellerMemberRole.Owner, owner.Role);
        Assert.Equal(0m, seller.Rating);
    }

    [Fact]
    public void AddManager_Twice_Throws()
    {
        var seller = new Seller("TechStore", Guid.NewGuid());
        var managerId = Guid.NewGuid();
        seller.AddManager(managerId);

        Assert.Throws<InvalidOperationException>(() => seller.AddManager(managerId));
    }

    [Fact]
    public void RemoveMember_Owner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var seller = new Seller("TechStore", ownerId);

        Assert.Throws<InvalidOperationException>(() => seller.RemoveMember(ownerId));
    }

    [Fact]
    public void ChangeRating_OutOfRange_Throws()
    {
        var seller = new Seller("TechStore", Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => seller.ChangeRating(5.01m));
    }
}
