using ClothingStore.Models.Entities;

namespace ClothingStore.Models.ViewModels;

public class ShipperDashboardViewModel
{
    public int WaitingCount { get; set; }
    public int ShippingCount { get; set; }
    public int DeliveredTodayCount { get; set; }
    public int FailedCount { get; set; }
    public int MonthlyDeliveredCount { get; set; }
    public int CodPendingCount { get; set; }
    public string? LongestShippingOrderCode { get; set; }
    public int LongestShippingDurationDays { get; set; }
    public double SuccessRate { get; set; }
    public int RetryWaitingCount { get; set; }
    public int UpcomingRetryCount { get; set; }
    public int OverdueRetryCount { get; set; }
}

public class ShipperOrderViewModel
{
    public int OrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AssignedAt { get; set; }
    public DateTime? AssignedAtLocal { get; set; }
    public DateTime? NextDeliveryDate { get; set; }
    public DateTime? NextDeliveryDateLocal { get; set; }
    public int DeliveryAttemptCount { get; set; }
}

public class ShipperOrderDetailViewModel : ShipperOrderViewModel
{
    public DateTime OrderDate { get; set; }
    public DateTime OrderDateLocal { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DeliveryFailureReasonCode { get; set; }
    public string? DeliveryFailureReason { get; set; }
    public string? DeliveryRescheduleReason { get; set; }
    public List<OrderDetailItemViewModel> Items { get; set; } = new List<OrderDetailItemViewModel>();
}

public class DeliveryStatusUpdateViewModel
{
    public string NewStatus { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public DateTime? NextDeliveryDate { get; set; }
    public string? DeliveryRescheduleReason { get; set; }
}
