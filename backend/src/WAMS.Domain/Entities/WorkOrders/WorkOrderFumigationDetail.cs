namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;

public class WorkOrderFumigationDetail : BaseEntity
{
    public long WorkOrderId { get; set; }
    public string? FumiId { get; set; }
    public string? TotalDuration { get; set; }
    public string? BlNumber { get; set; }
    public string? MvName { get; set; }
    public decimal? InitialTemperature { get; set; }
    public decimal? FinalTemperature { get; set; }
    public string? FumigationType { get; set; }
    public decimal? MethylBromideDosage { get; set; }
    public decimal? SulphurFluorideDosage { get; set; }
    public decimal? PhosphineDosage { get; set; }
    public string? Result { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}
