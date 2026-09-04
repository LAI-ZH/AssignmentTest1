namespace AssignmentTest1.Models.ViewModels
{
    public class QRPaymentConfirmModel
    {
        public string? TransactionId { get; set; }
        public string? PlanName { get; set; }
        public decimal? Price { get; set; }
        public bool AutoRenew { get; set; }
    }
}