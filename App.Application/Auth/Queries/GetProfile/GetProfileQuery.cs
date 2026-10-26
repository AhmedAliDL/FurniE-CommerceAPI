using App.Application.Auth.Dto;
using MediatR;

namespace App.Application.Auth.Queries.GetProfile
{
    public record GetProfileQuery : IRequest<ProfileDto?>;
}
