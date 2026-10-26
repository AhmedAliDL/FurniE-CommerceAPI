using App.Application.Responses;
using MediatR;

namespace App.Application.Checkout.Command.PlaceOrder
{
    public record PlaceOrderCommand : IRequest<ResponseResult<Guid>>
    {
        public string? City { get; set; }

        public string? Address { get; set; }

        public string PaymentMethod { get; set; } = null!;

        public string? CardNumber { get; set; }

        public int? ExpirationMonth { get; set; }

        public string? ExpirationYear { get; set; }

        public string? CVV { get; set; }

        public string? CouponCode { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal OrderDiscount { get; set; }

        public string? IdempotencyKey { get; set; }
    }
}
