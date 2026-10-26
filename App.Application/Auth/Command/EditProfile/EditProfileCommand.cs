using MediatR;
using Microsoft.AspNetCore.Identity;

namespace App.Application.Auth.Command.EditProfile
{
    public record EditProfileCommand : IRequest<IdentityResult>
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
        //[CheckImageExtension(errorMessage: "Invalid image file format. Only .jpg, .png, and .jpeg files are allowed.")]
        public string? ImagePath { get; set; }
    }
}
