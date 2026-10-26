namespace App.Application.Payments.Dto
{
    public class PaymentSessionDto
    {
        public string IntentionId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public List<Paymob.PaymobPaymentMethodDto> PaymentMethods { get; set; } = [];
    }
}
