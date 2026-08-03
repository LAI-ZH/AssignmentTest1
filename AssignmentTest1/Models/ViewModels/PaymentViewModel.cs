using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxBookings { get; set; }
        public int PTSessions { get; set; }
        public string? Benefits { get; set; }
        public string? Description { get; set; }
        public bool IncludesGymAccess { get; set; }
        public bool IncludesInbody { get; set; }

        [Required(ErrorMessage = "Please select a payment method")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Card";

        // Card 支付字段
        [Display(Name = "Card Number")]
        [StringLength(19, MinimumLength = 16, ErrorMessage = "Please enter a valid 16-digit card number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Date")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Please enter valid MM/YY format")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVV")]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression(@"^[0-9]{3}$", ErrorMessage = "Please enter a valid 3-digit CVV")]
        public string? CVV { get; set; }

        [Display(Name = "Cardholder Name")]
        [StringLength(100)]
        public string? CardholderName { get; set; }
    }
}