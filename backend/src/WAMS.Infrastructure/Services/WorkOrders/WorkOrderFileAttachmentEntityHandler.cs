namespace WAMS.Infrastructure.Services.WorkOrders;

using WAMS.Application.Interfaces.Files;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public sealed class WorkOrderFileAttachmentEntityHandler(
    IWorkOrderRepository workOrderRepository,
    IUserRepository userRepository) : IFileAttachmentEntityHandler
{
    public string EntityType => "work-orders";

    public async Task<FileAttachmentEntityContext?> ResolveAsync(long userId, long entityId, CancellationToken ct = default)
    {
        var ctx = await workOrderRepository.GetForAttachmentAsync(entityId, ct);
        if (ctx is null) return null;

        // Attachments inherit the work order's warehouse access check, so a tenant member can't
        // download another warehouse's documents just by knowing a work order id.
        var (_, hasAccess) = await userRepository.CheckWarehouseAccessAsync(userId, ctx.WarehouseShadowId, ct);
        if (!hasAccess)
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);

        return new FileAttachmentEntityContext(EntityType, ctx.Id, ctx.CompanyId)
        {
            OwnerUserId = ctx.CreatedByUserId,
            CanModify = ctx.CanBeEdited
        };
    }
}
