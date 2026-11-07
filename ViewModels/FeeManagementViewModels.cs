using System.ComponentModel.DataAnnotations;
using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class FeePlanListViewModel
{
    public IReadOnlyList<FeePlan> Plans { get; init; } = Array.Empty<FeePlan>();
    public string? Search { get; init; }
    public bool OutstandingOnly { get; init; }
}

public sealed class FeePlanFormViewModel
{
    [Range(1, int.MaxValue)]
    [Display(Name = "Student")]
    public int StudentId { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    [Display(Name = "Total fee")]
    public decimal TotalAmount { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    [Display(Name = "Discount / concession")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [Display(Name = "Due date")]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

    [Required, StringLength(20)]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = "";

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class RecordFeePaymentViewModel
{
    public int FeePlanId { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    [Display(Name = "Payment amount")]
    public decimal Amount { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(300)]
    [Display(Name = "Transaction reference")]
    public string? TransactionReference { get; set; }

    public string StudentName { get; set; } = "";
    public string AdmissionNumber { get; set; } = "";
    public decimal BalanceBeforePayment { get; set; }
}

public sealed class FeeReceiptViewModel
{
    public FeePayment Payment { get; init; } = null!;
    public DateTime PaidAtIndia { get; init; }
}
