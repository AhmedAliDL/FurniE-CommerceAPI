using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Payments.Command.VoidPayment
{
    public record VoidPaymentCommand : IRequest<ResponseResult<PaymentOperationResultDto>>
    {
        public Guid OrderId { get; init; }
    }
}
