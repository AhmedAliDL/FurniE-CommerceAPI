using App.Api.Controllers;
using App.Application.Auth.Command.ChangePassword;
using App.Application.Auth.Command.EditProfile;
using App.Application.Auth.Command.Login;
using App.Application.Auth.Command.RefreshToken;
using App.Application.Auth.Command.Register;
using App.Application.Auth.Command.ResetPassword;
using App.Application.Auth.Dto;
using App.Application.Auth.Queries.GetProfile;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Auth")]
    public class AuthControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private AuthController _controller = null!;
        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new AuthController(_sender.Object);
        }
        [Test]
        public async Task EnsureThatProfileReturnsOkWhenProfileExists()
        {
            var profile = new ProfileDto
            {
                FName = "Ahmed",
                LName = "Ali",
                Address = "cairo,nasr city",
                Email = "ahmedAli@gmail.com"
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProfileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            var result = await _controller.Profile();

            Assert.That(result,
                Is.TypeOf<OkObjectResult>());

        }
        [Test]
        public async Task EnsureThatProfileReturnsNoContentWhenProfileNotExists()
        {

            _sender.Setup(s => s.Send(
                    It.IsAny<GetProfileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProfileDto)null!);

            var result = await _controller.Profile();

            Assert.That(result,
                Is.TypeOf<NoContentResult>());

        }

        [Test]
        public async Task EnsureThatRegisterReturnsOkWhenRegisterSucceeds()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<RegisterCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.Register(new RegisterCommand { FName = "Ahmed", LName = "Ali", Email = "ahmed@gmail.com", Password = "P@ssw0rd", ConfirmPassword = "P@ssw0rd" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatRegisterReturnsBadRequestWhenRegisterFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<RegisterCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.Empty);

            var result = await _controller.Register(new RegisterCommand { FName = "Ahmed", LName = "Ali", Email = "ahmed@gmail.com", Password = "P@ssw0rd", ConfirmPassword = "P@ssw0rd" });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatLoginReturnsOkWhenTokenIssued()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<LoginCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginDto
                {
                    Token = "access-token",
                    RefreshToken = "refresh-token",
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(1)
                });

            var result = await _controller.Login(new LoginCommand { Email = "ahmed@gmail.com", Password = "P@ssw0rd" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatLoginReturnsBadRequestWhenNoRefreshTokenIssued()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<LoginCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginDto { Token = "access-token" });

            var result = await _controller.Login(new LoginCommand { Email = "ahmed@gmail.com", Password = "P@ssw0rd" });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatEditProfileReturnsOkWhenUpdateSucceeds()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<EditProfileCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

            var result = await _controller.EditProfile(new EditProfileCommand { FName = "Ahmed" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatEditProfileReturnsBadRequestWhenUpdateFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<EditProfileCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "Invalid phone number." }));

            var result = await _controller.EditProfile(new EditProfileCommand { FName = "Ahmed" });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatChangePasswordReturnsOkWhenPasswordChanged()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ChangePasswordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.ChangePassword(new ChangePasswordCommand { OldPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatChangePasswordReturnsBadRequestWhenChangeFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ChangePasswordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.ChangePassword(new ChangePasswordCommand { OldPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatResetPasswordReturnsOkWhenPasswordReset()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ResetPasswordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.ResetPassword(new ResetPasswordCommand { ConfirmationToken = "token", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatResetPasswordReturnsBadRequestWhenResetFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ResetPasswordCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.ResetPassword(new ResetPasswordCommand { ConfirmationToken = "token", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRefreshTokenReturnsOkWhenTokenRefreshed()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<RefreshTokenCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<RefreshTokenDto>.Success(new RefreshTokenDto
                {
                    Token = "new-access-token",
                    RefreshToken = "new-refresh-token",
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(1)
                }));

            var result = await _controller.RefreshToken();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(_controller.Response.Headers.SetCookie.ToString(), Does.Contain("RefreshToken=new-refresh-token"));
        }

        [Test]
        public async Task EnsureThatRefreshTokenReturnsBadRequestWhenRefreshFails()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<RefreshTokenCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<RefreshTokenDto>.Failure("Invalid refresh token."));

            var result = await _controller.RefreshToken();

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatLogoutReturnsNoContentWhenNoRefreshTokenCookie()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.Logout();

            Assert.That(result, Is.TypeOf<NoContentResult>());
            _sender.Verify(s => s.Send(It.IsAny<App.Application.Auth.Command.Logout.LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatSendPhoneOtpReturnsContentWhenOtpSent()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<App.Application.Auth.Command.SendOTPForPhoneVerification.SendOTPForPhoneVerificationCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("OTP sent successfully.");

            var result = await _controller.SendPhoneOTP(new App.Application.Auth.Command.SendOTPForPhoneVerification.SendOTPForPhoneVerificationCommand("01012345678"));

            Assert.That(result, Is.TypeOf<ContentResult>());
        }

        [Test]
        public async Task EnsureThatVerifyPhoneReturnsContentWhenOtpVerified()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<App.Application.Auth.Command.VerifyOTPForPhoneVerification.VerifyOTPForPhoneVerificationCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("Phone verified successfully.");

            var result = await _controller.VerifyPhone(new App.Application.Auth.Command.VerifyOTPForPhoneVerification.VerifyOTPForPhoneVerificationCommand("123456"));

            Assert.That(result, Is.TypeOf<ContentResult>());
        }

    }
}
