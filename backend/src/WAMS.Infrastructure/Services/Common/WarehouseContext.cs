namespace WAMS.Infrastructure.Services.Common;

using WAMS.Application.Interfaces.Warehouses;

public sealed class WarehouseContext : IWarehouseContext
{
    private long? _warehouseId;
    private bool _isSet;

    public long? WarehouseId => _isSet ? _warehouseId : null;

    public bool IsSet => _isSet;

    public void SetWarehouseId(long warehouseId)
    {
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId must be a positive number");
        _warehouseId = warehouseId;
        _isSet = true;
    }

    public void SetBypassMode()
    {
        _warehouseId = null;
        _isSet = true;
    }
}
