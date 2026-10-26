using App.Application.Checkout.Dto;
using MediatR;

namespace App.Application.Checkout.Queries.GetCheckoutDetails
{
    public record GetCheckoutDetailsQuery : IRequest<CheckoutDto>;
}
