namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderTransportOrderConfiguration : IEntityTypeConfiguration<WorkOrderTransportOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrderTransportOrder> builder)
    {
        builder.ToTable("work_order_transport_orders");
        builder.HasKey(x => new { x.WorkOrderId, x.TransportOrderShadowId });

        builder.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(x => x.TransportOrderShadowId).HasColumnName("transport_order_shadow_id");

        builder.HasOne(x => x.WorkOrder)
            .WithMany(w => w.TransportOrders)
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TransportOrderShadow)
            .WithMany()
            .HasForeignKey(x => x.TransportOrderShadowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
