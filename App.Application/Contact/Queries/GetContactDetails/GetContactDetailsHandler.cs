using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Contact.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Contact.Queries.GetContactDetails
{
    public class GetContactDetailsHandler(IIdentityService identityService, ICurrentUserService currentUserService, ILogger<GetContactDetailsHandler> logger) : IRequestHandler<GetContactDetailsQuery, ContactDetailsDto>
    {
        public async Task<ContactDetailsDto> Handle(GetContactDetailsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get contact details operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var user = await identityService.GetUserByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            logger.LogInformation("Get contact details operation completed.");
            return new ContactDetailsDto
            {
                CustomerName = $"{user.FName} {user.LName}".Trim(),
                Email = user.Email!,
                Phone = user.PhoneNumber
            };
        }
    }
}
