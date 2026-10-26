using App.Application.Contact.Dto;
using MediatR;

namespace App.Application.Contact.Queries.GetContactDetails
{
    public record GetContactDetailsQuery : IRequest<ContactDetailsDto>;
}
