using App.Application.Auth.Command.ChangePassword;
using App.Application.Auth.Command.ConfirmEmail;
using App.Application.Auth.Command.EditProfile;
using App.Application.Auth.Command.ForgetPassword;
using App.Application.Auth.Command.Login;
using App.Application.Auth.Command.Logout;
using App.Application.Auth.Command.RefreshToken;
using App.Application.Auth.Command.Register;
using App.Application.Auth.Command.ResetPassword;
using App.Application.Auth.Command.SendEmailConfirmation;
using App.Application.Auth.Dto;
using App.Application.Auth.Queries.GetProfile;
using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using App.Application.Notifications.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Auth")]
    public class AuthHandlerTest
    {
        private Mock<IIdentityService> _identityService = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IJwtTokenService> _jwtTokenService = null!;
        private Mock<IRoleService> _roleService = null!;
        private Mock<IContactService> _contactService = null!;
        private Mock<IEmailTemplateBuilder> _emailTemplateBuilder = null!;
        private IConfiguration _configuration = null!;

        private Mock<ILogger<GetProfileHandler>> _getProfileLogger = null!;
        private Mock<ILogger<LoginHandler>> _loginLogger = null!;
        private Mock<ILogger<RegisterHandler>> _registerLogger = null!;
        private Mock<ILogger<ChangePasswordHandler>> _changePasswordLogger = null!;
        private Mock<ILogger<EditProfileHandler>> _editProfileLogger = null!;
        private Mock<ILogger<ResetPasswordHandler>> _resetPasswordLogger = null!;
        private Mock<ILogger<ForgetPasswordHandler>> _forgetPasswordLogger = null!;
        private Mock<ILogger<ConfirmEmailHandler>> _confirmEmailLogger = null!;
        private Mock<ILogger<SendEmailConfirmationHandler>> _sendEmailConfirmationLogger = null!;
        private Mock<ILogger<RefreshTokenHandler>> _refreshTokenLogger = null!;
        private Mock<ILogger<LogoutHandler>> _logoutLogger = null!;

        private GetProfileHandler _getProfileHandler = null!;
        private LoginHandler _loginHandler = null!;
        private RegisterHandler _registerHandler = null!;
        private ChangePasswordHandler _changePasswordHandler = null!;
        private EditProfileHandler _editProfileHandler = null!;
        private ResetPasswordHandler _resetPasswordHandler = null!;
        private ForgetPasswordHandler _forgetPasswordHandler = null!;
        private ConfirmEmailHandler _confirmEmailHandler = null!;
        private SendEmailConfirmationHandler _sendEmailConfirmationHandler = null!;
        private RefreshTokenHandler _refreshTokenHandler = null!;
        private LogoutHandler _logoutHandler = null!;

        [SetUp]
        public void Setup()
        {
            _getProfileLogger = new Mock<ILogger<GetProfileHandler>>();
            _identityService = new Mock<IIdentityService>();
            _currentUserService = new Mock<ICurrentUserService>();
            _jwtTokenService = new Mock<IJwtTokenService>();
            _roleService = new Mock<IRoleService>();
            _contactService = new Mock<IContactService>();
            _emailTemplateBuilder = new Mock<IEmailTemplateBuilder>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SmtpSettings:AdminEmail", "admin@furni.com" }
                })
                .Build();

            _loginLogger = new Mock<ILogger<LoginHandler>>();
            _registerLogger = new Mock<ILogger<RegisterHandler>>();
            _changePasswordLogger = new Mock<ILogger<ChangePasswordHandler>>();
            _editProfileLogger = new Mock<ILogger<EditProfileHandler>>();
            _resetPasswordLogger = new Mock<ILogger<ResetPasswordHandler>>();
            _forgetPasswordLogger = new Mock<ILogger<ForgetPasswordHandler>>();
            _confirmEmailLogger = new Mock<ILogger<ConfirmEmailHandler>>();
            _sendEmailConfirmationLogger = new Mock<ILogger<SendEmailConfirmationHandler>>();
            _refreshTokenLogger = new Mock<ILogger<RefreshTokenHandler>>();
            _logoutLogger = new Mock<ILogger<LogoutHandler>>();

            _getProfileHandler = new GetProfileHandler(
                _identityService.Object,
                _currentUserService.Object,
                _getProfileLogger.Object
                );
            _loginHandler = new LoginHandler(
                _identityService.Object,
                _jwtTokenService.Object,
                _loginLogger.Object);
            _registerHandler = new RegisterHandler(
                _identityService.Object,
                _roleService.Object,
                _registerLogger.Object);
            _changePasswordHandler = new ChangePasswordHandler(
                _identityService.Object,
                _currentUserService.Object,
                _changePasswordLogger.Object);
            _editProfileHandler = new EditProfileHandler(
                _identityService.Object,
                _currentUserService.Object,
                _editProfileLogger.Object);
            _resetPasswordHandler = new ResetPasswordHandler(
                _identityService.Object,
                _currentUserService.Object,
                _resetPasswordLogger.Object);
            _forgetPasswordHandler = new ForgetPasswordHandler(
                _configuration,
                _contactService.Object,
                _identityService.Object,
                _emailTemplateBuilder.Object,
                _forgetPasswordLogger.Object);
            _confirmEmailHandler = new ConfirmEmailHandler(
                _identityService.Object,
                _confirmEmailLogger.Object);
            _sendEmailConfirmationHandler = new SendEmailConfirmationHandler(
                _identityService.Object,
                _contactService.Object,
                _configuration,
                _emailTemplateBuilder.Object,
                _sendEmailConfirmationLogger.Object);
            _refreshTokenHandler = new RefreshTokenHandler(
                _jwtTokenService.Object,
                _refreshTokenLogger.Object);
            _logoutHandler = new LogoutHandler(
                _jwtTokenService.Object,
                _logoutLogger.Object);
        }

        private AppUser ValidUser()
        {
            return new AppUser
            {
                Id = Guid.NewGuid(),
                FName = "Ahmed",
                LName = "Ali",
                Address = "cairo,nasr city",
                Email = "ahmedAli@gmail.com",
                EmailConfirmed = true,
                RefreshTokens = new List<RefreshToken>()
            };
        }

        [Test]
        public async Task EnsureGetProfileHandlerReturnsPrfoileWhenUserExists()
        {

            Guid userId = Guid.NewGuid();
            AppUser user = new AppUser
            {
                Id = userId,
                FName = "Ahmed",
                LName = "Ali",
                Address = "cairo,nasr city",
                Email = "ahmedAli@gmail.com",
            };
            _currentUserService.Setup(x => x.UserId)
                .Returns(userId);

            _identityService
                .Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            var result = await _getProfileHandler.Handle(
               It.IsAny<GetProfileQuery>(),
               It.IsAny<CancellationToken>());

            Assert.That(result!.Email, Is.EqualTo(user.Email));

        }

        [Test]
        public async Task EnsureLoginHandlerReturnsTokenAndNewRefreshTokenWhenUserHasNoActiveRefreshToken()
        {
            AppUser user = ValidUser();
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);
            _identityService.Setup(x => x.CheckCorrectnessOfPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
            _identityService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            JwtSecurityToken jwtToken = new JwtSecurityToken("issuer", "audience", expires: DateTime.UtcNow.AddHours(1));
            _jwtTokenService.Setup(x => x.CreateJWTTokenAsync(user)).ReturnsAsync(jwtToken);
            RefreshToken refreshToken = new RefreshToken { Token = "rt-1", ExpiresOn = DateTime.UtcNow.AddDays(7) };
            _jwtTokenService.Setup(x => x.CreateRefreshToken()).Returns(refreshToken);

            LoginCommand command = new LoginCommand { Email = user.Email!, Password = "Pass@123" };

            var result = await _loginHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Token, Is.EqualTo(new JwtSecurityTokenHandler().WriteToken(jwtToken)));
                Assert.That(result.Expiration, Is.EqualTo(jwtToken.ValidTo));
                Assert.That(result.RefreshToken, Is.EqualTo("rt-1"));
                Assert.That(result.RefreshTokenExpiration, Is.EqualTo(refreshToken.ExpiresOn));
                Assert.That(user.RefreshTokens.Count, Is.EqualTo(1));
                Assert.That(user.LastLoginAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
            });
            _jwtTokenService.Verify(x => x.CreateRefreshToken(), Times.Once);
            _identityService.Verify(x => x.UpdateUserAsync(user), Times.Once);
        }

        [Test]
        public async Task EnsureLoginHandlerReusesActiveRefreshTokenWhenUserHasOne()
        {
            AppUser user = ValidUser();
            RefreshToken activeToken = new RefreshToken { Token = "rt-active", ExpiresOn = DateTime.UtcNow.AddDays(5) };
            user.RefreshTokens = new List<RefreshToken> { activeToken };

            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);
            _identityService.Setup(x => x.CheckCorrectnessOfPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
            _identityService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);
            _jwtTokenService.Setup(x => x.CreateJWTTokenAsync(user)).ReturnsAsync(new JwtSecurityToken("issuer", "audience", expires: DateTime.UtcNow.AddHours(1)));

            LoginCommand command = new LoginCommand { Email = user.Email!, Password = "Pass@123" };

            var result = await _loginHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.RefreshToken, Is.EqualTo("rt-active"));
                Assert.That(result.RefreshTokenExpiration, Is.EqualTo(activeToken.ExpiresOn));
                Assert.That(user.RefreshTokens.Count, Is.EqualTo(1));
            });
            _jwtTokenService.Verify(x => x.CreateRefreshToken(), Times.Never);
        }

        [Test]
        public void EnsureLoginHandlerThrowsForbiddenExceptionWhenEmailIsInvalid()
        {
            _identityService.Setup(x => x.GetUserByEmailAsync("unknown@gmail.com")).ReturnsAsync((AppUser?)null);

            LoginCommand command = new LoginCommand { Email = "unknown@gmail.com", Password = "Pass@123" };

            Assert.ThrowsAsync<ForbiddenException>(async () => await _loginHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureLoginHandlerThrowsForbiddenExceptionWhenAccountIsDeactivated()
        {
            AppUser user = ValidUser();
            user.IsActive = false;
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);

            LoginCommand command = new LoginCommand { Email = user.Email!, Password = "Pass@123" };

            Assert.ThrowsAsync<ForbiddenException>(async () => await _loginHandler.Handle(command, It.IsAny<CancellationToken>()));
            _identityService.Verify(x => x.CheckCorrectnessOfPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
            _jwtTokenService.Verify(x => x.CreateJWTTokenAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Test]
        public void EnsureLoginHandlerThrowsForbiddenExceptionWhenEmailIsNotConfirmed()
        {
            AppUser user = ValidUser();
            user.EmailConfirmed = false;
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);

            LoginCommand command = new LoginCommand { Email = user.Email!, Password = "Pass@123" };

            Assert.ThrowsAsync<ForbiddenException>(async () => await _loginHandler.Handle(command, It.IsAny<CancellationToken>()));
            _identityService.Verify(x => x.CheckCorrectnessOfPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void EnsureLoginHandlerThrowsForbiddenExceptionWhenPasswordIsIncorrect()
        {
            AppUser user = ValidUser();
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);
            _identityService.Setup(x => x.CheckCorrectnessOfPasswordAsync(user, "Wrong@123")).ReturnsAsync(false);

            LoginCommand command = new LoginCommand { Email = user.Email!, Password = "Wrong@123" };

            Assert.ThrowsAsync<ForbiddenException>(async () => await _loginHandler.Handle(command, It.IsAny<CancellationToken>()));
            _jwtTokenService.Verify(x => x.CreateJWTTokenAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Test]
        public async Task EnsureRegisterHandlerAssignsExistingUserRoleWhenRoleExists()
        {
            _identityService.Setup(x => x.CreateUserAsync(It.IsAny<AppUser>(), "Pass@123")).ReturnsAsync(IdentityResult.Success);
            _roleService.Setup(x => x.IsRoleExistsAsync(SupportRoles.Customer)).ReturnsAsync(true);
            _roleService.Setup(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), SupportRoles.Customer)).ReturnsAsync(IdentityResult.Success);

            RegisterCommand command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Ali",
                Email = "ahmedAli@gmail.com",
                Phone = "01001234567",
                Address = "cairo, nasr city",
                Password = "Pass@123",
                ConfirmPassword = "Pass@123"
            };

            await _registerHandler.Handle(command, It.IsAny<CancellationToken>());

            _identityService.Verify(x => x.CreateUserAsync(It.Is<AppUser>(u =>
                u.FName == "Ahmed" &&
                u.LName == "Ali" &&
                u.Email == "ahmedAli@gmail.com" &&
                u.UserName == "ahmedAli@gmail.com" &&
                u.PhoneNumber == "01001234567" &&
                u.Address == "cairo, nasr city"), "Pass@123"), Times.Once);
            _roleService.Verify(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), SupportRoles.Customer), Times.Once);
            _roleService.Verify(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task EnsureRegisterHandlerCreatesUserRoleWhenRoleDoesNotExist()
        {
            _identityService.Setup(x => x.CreateUserAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _roleService.Setup(x => x.IsRoleExistsAsync(SupportRoles.Customer)).ReturnsAsync(false);
            _roleService.Setup(x => x.CreateRoleAsync(SupportRoles.Customer, "normal user for the system.")).ReturnsAsync(IdentityResult.Success);
            _roleService.Setup(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), SupportRoles.Customer)).ReturnsAsync(IdentityResult.Success);

            RegisterCommand command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Ali",
                Email = "ahmedAli@gmail.com",
                Address = "cairo, nasr city",
                Password = "Pass@123",
                ConfirmPassword = "Pass@123"
            };

            await _registerHandler.Handle(command, It.IsAny<CancellationToken>());

            _roleService.Verify(x => x.CreateRoleAsync(SupportRoles.Customer, "normal user for the system."), Times.Once);
            _roleService.Verify(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), SupportRoles.Customer), Times.Once);
        }

        [Test]
        public void EnsureRegisterHandlerThrowsInvalidOperationExceptionWhenRoleCreationFails()
        {
            _identityService.Setup(x => x.CreateUserAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _roleService.Setup(x => x.IsRoleExistsAsync(SupportRoles.Customer)).ReturnsAsync(false);
            _roleService.Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "role creation failed" }));

            RegisterCommand command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Ali",
                Email = "ahmedAli@gmail.com",
                Address = "cairo, nasr city",
                Password = "Pass@123",
                ConfirmPassword = "Pass@123"
            };

            Assert.ThrowsAsync<InvalidOperationException>(async () => await _registerHandler.Handle(command, It.IsAny<CancellationToken>()));
            _roleService.Verify(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task EnsureRegisterHandlerSkipsRoleAssignmentWhenUserCreationFails()
        {
            _identityService.Setup(x => x.CreateUserAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "creation failed" }));

            RegisterCommand command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Ali",
                Email = "ahmedAli@gmail.com",
                Address = "cairo, nasr city",
                Password = "Pass@123",
                ConfirmPassword = "Pass@123"
            };

            await _registerHandler.Handle(command, It.IsAny<CancellationToken>());

            _roleService.Verify(x => x.IsRoleExistsAsync(It.IsAny<string>()), Times.Never);
            _roleService.Verify(x => x.AssignRoleToUserAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task EnsureChangePasswordHandlerReturnsTrueWhenPasswordChanged()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.ChangeUserPasswordAsync(user, "Old@123", "New@1234")).ReturnsAsync(true);

            ChangePasswordCommand command = new ChangePasswordCommand { OldPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            var result = await _changePasswordHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _identityService.Verify(x => x.ChangeUserPasswordAsync(user, "Old@123", "New@1234"), Times.Once);
        }

        [Test]
        public async Task EnsureChangePasswordHandlerReturnsFalseWhenPasswordChangeFails()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.ChangeUserPasswordAsync(user, "Old@123", "New@1234")).ReturnsAsync(false);

            ChangePasswordCommand command = new ChangePasswordCommand { OldPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            var result = await _changePasswordHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
        }

        [Test]
        public void EnsureChangePasswordHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            Guid userId = Guid.NewGuid();
            _currentUserService.Setup(x => x.UserId).Returns(userId);
            _identityService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((AppUser?)null);

            ChangePasswordCommand command = new ChangePasswordCommand { OldPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            Assert.ThrowsAsync<NotFoundException>(async () => await _changePasswordHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureEditProfileHandlerUpdatesProfileFieldsWhenNewValuesProvided()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            EditProfileCommand command = new EditProfileCommand { FName = "Mohamed", Address = "giza, dokki" };

            var result = await _editProfileHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result.Succeeded, Is.True);
            _identityService.Verify(x => x.UpdateUserAsync(It.Is<AppUser>(u =>
                u.FName == "Mohamed" &&
                u.LName == "Ali" &&
                u.Address == "giza, dokki" &&
                u.Email == "ahmedAli@gmail.com")), Times.Once);
            _identityService.Verify(x => x.ChangeUserPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task EnsureEditProfileHandlerChangesPasswordWhenCurrentAndNewPasswordProvided()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.ChangeUserPasswordAsync(user, "Old@123", "New@1234")).ReturnsAsync(true);
            _identityService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            EditProfileCommand command = new EditProfileCommand { CurrentPassword = "Old@123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            var result = await _editProfileHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result.Succeeded, Is.True);
            _identityService.Verify(x => x.ChangeUserPasswordAsync(user, "Old@123", "New@1234"), Times.Once);
            _identityService.Verify(x => x.UpdateUserAsync(user), Times.Once);
        }

        [Test]
        public void EnsureEditProfileHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            Guid userId = Guid.NewGuid();
            _currentUserService.Setup(x => x.UserId).Returns(userId);
            _identityService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((AppUser?)null);

            EditProfileCommand command = new EditProfileCommand { FName = "Mohamed" };

            Assert.ThrowsAsync<NotFoundException>(async () => await _editProfileHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureResetPasswordHandlerReturnsTrueWhenPasswordReset()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.ResetUserPasswordAsync(user, "tok-123", "New@1234")).ReturnsAsync(true);

            ResetPasswordCommand command = new ResetPasswordCommand { ConfirmationToken = "tok-123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            var result = await _resetPasswordHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _identityService.Verify(x => x.ResetUserPasswordAsync(user, "tok-123", "New@1234"), Times.Once);
        }

        [Test]
        public async Task EnsureResetPasswordHandlerReturnsFalseWhenPasswordResetFails()
        {
            AppUser user = ValidUser();
            _currentUserService.Setup(x => x.UserId).Returns(user.Id);
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.ResetUserPasswordAsync(user, "tok-123", "New@1234")).ReturnsAsync(false);

            ResetPasswordCommand command = new ResetPasswordCommand { ConfirmationToken = "tok-123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            var result = await _resetPasswordHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
        }

        [Test]
        public void EnsureResetPasswordHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            Guid userId = Guid.NewGuid();
            _currentUserService.Setup(x => x.UserId).Returns(userId);
            _identityService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((AppUser?)null);

            ResetPasswordCommand command = new ResetPasswordCommand { ConfirmationToken = "tok-123", NewPassword = "New@1234", ConfirmNewPassword = "New@1234" };

            Assert.ThrowsAsync<NotFoundException>(async () => await _resetPasswordHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureForgetPasswordHandlerSendsResetPasswordEmailWhenUserExists()
        {
            AppUser user = ValidUser();
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);
            _identityService.Setup(x => x.GenerateForgetPasswordConfirmationTokenAsync(user)).ReturnsAsync("tok-123");
            _emailTemplateBuilder.Setup(x => x.Build(It.Is<EmailContent>(e =>
                e.Title == "Reset Your Password" &&
                e.GreetingName == "Ahmed" &&
                e.Highlight == "tok-123"))).Returns("<html>Reset your password</html>");
            _contactService.Setup(x => x.SendEmailServiceAsync(
                user.Email!,
                "admin@furni.com",
                "Reset Your Password - Furni",
                "<html>Reset your password</html>",
                "Furni",
                "Ahmed Ali",
                true,
                It.IsAny<string?>(),
                It.IsAny<string?>())).ReturnsAsync(new NotifyDto { Success = true, Message = "Email sent successfully" });

            ForgetPasswordCommand command = new ForgetPasswordCommand(user.Email!);

            var result = await _forgetPasswordHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Is.EqualTo("Email sent successfully"));
            });
            _contactService.Verify(x => x.SendEmailServiceAsync(
                user.Email!,
                "admin@furni.com",
                "Reset Your Password - Furni",
                "<html>Reset your password</html>",
                "Furni",
                "Ahmed Ali",
                true,
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }

        [Test]
        public void EnsureForgetPasswordHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            _identityService.Setup(x => x.GetUserByEmailAsync("unknown@gmail.com")).ReturnsAsync((AppUser?)null);

            ForgetPasswordCommand command = new ForgetPasswordCommand("unknown@gmail.com");

            Assert.ThrowsAsync<NotFoundException>(async () => await _forgetPasswordHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureConfirmEmailHandlerConfirmsEmailWhenTokenIsValid()
        {
            AppUser user = ValidUser();
            user.EmailConfirmed = false;
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.CheckEmailConfirmationTokenAsync(user, "tok-123")).ReturnsAsync(true);
            _identityService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            ConfirmEmailCommand command = new ConfirmEmailCommand(user.Id, "tok-123");

            var result = await _confirmEmailHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result.Succeeded, Is.True);
            _identityService.Verify(x => x.UpdateUserAsync(It.Is<AppUser>(u => u.EmailConfirmed)), Times.Once);
        }

        [Test]
        public void EnsureConfirmEmailHandlerThrowsArgumentExceptionWhenTokenIsInvalid()
        {
            AppUser user = ValidUser();
            _identityService.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(x => x.CheckEmailConfirmationTokenAsync(user, "bad-token")).ReturnsAsync(false);

            ConfirmEmailCommand command = new ConfirmEmailCommand(user.Id, "bad-token");

            Assert.ThrowsAsync<ArgumentException>(async () => await _confirmEmailHandler.Handle(command, It.IsAny<CancellationToken>()));
            _identityService.Verify(x => x.UpdateUserAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Test]
        public void EnsureConfirmEmailHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            Guid userId = Guid.NewGuid();
            _identityService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((AppUser?)null);

            ConfirmEmailCommand command = new ConfirmEmailCommand(userId, "tok-123");

            Assert.ThrowsAsync<NotFoundException>(async () => await _confirmEmailHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureSendEmailConfirmationHandlerSendsConfirmationEmailWhenUserExists()
        {
            AppUser user = ValidUser();
            _identityService.Setup(x => x.GetUserByEmailAsync(user.Email!)).ReturnsAsync(user);
            _identityService.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("tok-456");
            _emailTemplateBuilder.Setup(x => x.Build(It.Is<EmailContent>(e =>
                e.Title == "Confirm Your Email Address" &&
                e.GreetingName == "Ahmed" &&
                e.Highlight == "tok-456"))).Returns("<html>Confirm your email</html>");
            _contactService.Setup(x => x.SendEmailServiceAsync(
                user.Email!,
                "admin@furni.com",
                "Confirm Your Email Address - Furni",
                "<html>Confirm your email</html>",
                "Furni",
                "Ahmed Ali",
                true,
                It.IsAny<string?>(),
                It.IsAny<string?>())).ReturnsAsync(new NotifyDto { Success = true, Message = "Email sent successfully" });

            SendEmailConfirmationCommand command = new SendEmailConfirmationCommand(user.Email!);

            var result = await _sendEmailConfirmationHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Is.EqualTo("Email sent successfully"));
            });
            _contactService.Verify(x => x.SendEmailServiceAsync(
                user.Email!,
                "admin@furni.com",
                "Confirm Your Email Address - Furni",
                "<html>Confirm your email</html>",
                "Furni",
                "Ahmed Ali",
                true,
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }

        [Test]
        public void EnsureSendEmailConfirmationHandlerThrowsNotFoundExceptionWhenUserNotFound()
        {
            _identityService.Setup(x => x.GetUserByEmailAsync("unknown@gmail.com")).ReturnsAsync((AppUser?)null);

            SendEmailConfirmationCommand command = new SendEmailConfirmationCommand("unknown@gmail.com");

            Assert.ThrowsAsync<NotFoundException>(async () => await _sendEmailConfirmationHandler.Handle(command, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureRefreshTokenHandlerReturnsSuccessWhenTokenServiceSucceeds()
        {
            RefreshTokenDto dto = new RefreshTokenDto
            {
                Token = "new-jwt",
                Expiration = DateTime.UtcNow.AddHours(1),
                RefreshToken = "rt-2",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
            };
            _jwtTokenService.Setup(x => x.RefreshTokenAsync("rt-1")).ReturnsAsync(ResponseResult<RefreshTokenDto>.Success(dto));

            RefreshTokenCommand command = new RefreshTokenCommand("rt-1");

            var result = await _refreshTokenHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value!.Token, Is.EqualTo("new-jwt"));
                Assert.That(result.Value.RefreshToken, Is.EqualTo("rt-2"));
            });
            _jwtTokenService.Verify(x => x.RefreshTokenAsync("rt-1"), Times.Once);
        }

        [Test]
        public async Task EnsureRefreshTokenHandlerReturnsFailureWhenTokenServiceFails()
        {
            _jwtTokenService.Setup(x => x.RefreshTokenAsync("rt-1"))
                .ReturnsAsync(ResponseResult<RefreshTokenDto>.Failure("Invalid refresh token."));

            RefreshTokenCommand command = new RefreshTokenCommand("rt-1");

            var result = await _refreshTokenHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Does.Contain("Invalid refresh token."));
            });
        }

        [Test]
        public async Task EnsureLogoutHandlerReturnsSuccessWhenTokenRevoked()
        {
            _jwtTokenService.Setup(x => x.RevokeTokenAsync("rt-1")).ReturnsAsync(ResponseResult<bool>.Success(true));

            LogoutCommand command = new LogoutCommand("rt-1");

            var result = await _logoutHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _jwtTokenService.Verify(x => x.RevokeTokenAsync("rt-1"), Times.Once);
        }

        [Test]
        public async Task EnsureLogoutHandlerReturnsFailureWhenTokenRevocationFails()
        {
            _jwtTokenService.Setup(x => x.RevokeTokenAsync("rt-1"))
                .ReturnsAsync(ResponseResult<bool>.Failure("Invalid refresh token."));

            LogoutCommand command = new LogoutCommand("rt-1");

            var result = await _logoutHandler.Handle(command, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            _jwtTokenService.Verify(x => x.RevokeTokenAsync("rt-1"), Times.Once);
        }
    }
}
