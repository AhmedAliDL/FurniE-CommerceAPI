namespace App.Application.Payments.Dto
{
    public class PaymentOperationResultDto
    {
        public long PaymobTransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal RefundedAmount { get; set; }
        public string? Message { get; set; }
    }
}
