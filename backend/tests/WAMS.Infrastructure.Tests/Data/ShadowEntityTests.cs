using FluentAssertions;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using Xunit;

namespace WAMS.Infrastructure.Tests.Data;

public class ShadowEntityTests
{
    [Fact]
    public void IShadowEntity_ShouldBeImplementedBy_WarehouseShadow()
    {
        typeof(WarehouseShadow).Should().Implement<IShadowEntity>();
    }

    [Fact]
    public void IShadowEntity_ShouldBeImplementedBy_ItemShadow()
    {
        typeof(ItemShadow).Should().Implement<IShadowEntity>();
    }

    [Fact]
    public void IShadowEntity_ShouldBeImplementedBy_VendorShadow()
    {
        typeof(VendorShadow).Should().Implement<IShadowEntity>();
    }

    [Fact]
    public void IShadowEntity_ShouldBeImplementedBy_SpkShadow()
    {
        typeof(SpkShadow).Should().Implement<IShadowEntity>();
    }
}
