using App.Application.Payments.Dto.Paymob;
using MediatR;

namespace App.Application.Payments.Command.HandlePaymobCallback
{
    // Returns true when a new transaction was recorded, false when it was a duplicate.
    public record HandlePaymobCallbackCommand(PaymobCallbackDto Body, string? Hmac) : IRequest<bool>;
}
