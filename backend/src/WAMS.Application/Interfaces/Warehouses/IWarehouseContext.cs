namespace WAMS.Application.Interfaces.Warehouses;

public interface IWarehouseContext
{
    /// <summary>
    /// The warehouse ID from the X-Warehouse-Id request header.
    /// Null when in bypass mode (Super Admin) or when the header was not sent.
    /// Never throws.
    /// </summary>
    long? WarehouseId { get; }

    /// <summary>
    /// True when the context has been explicitly set by WarehouseMiddleware
    /// (either a specific warehouse ID or Super Admin bypass).
    /// </summary>
    bool IsSet { get; }

    void SetWarehouseId(long warehouseId);

    /// <summary>
    /// Called for Super Admin: warehouse filter is disabled, all records are visible.
    /// </summary>
    void SetBypassMode();
}
