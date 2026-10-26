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
using App.Application.Auth.Command.SendOTPForPhoneVerification;
using App.Application.Auth.Command.VerifyOTPForPhoneVerification;
using App.Application.Common.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Auth")]
    public class AuthValidatorsTest
    {
        private Mock<IIdentityService> _identityService = null!;
        private RegisterValidator _registerValidator = null!;
        private EditProfileValidator _editProfileValidator = null!;

        [SetUp]
        public void Setup()
        {
            _identityService = new Mock<IIdentityService>();
            _registerValidator = new RegisterValidator(_identityService.Object);
            _editProfileValidator = new EditProfileValidator(_identityService.Object);
        }

        [Test]
        public void EnsureThatConfirmEmailCommandIsValidWhenAllFieldsProvided()
        {
            var validator = new ConfirmEmailValidator();
            var command = new ConfirmEmailCommand(Guid.NewGuid(), "confirmation-token");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatConfirmEmailCommandIsInvalidWhenTokenMissing()
        {
            var validator = new ConfirmEmailValidator();
            var command = new ConfirmEmailCommand(Guid.NewGuid(), "");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRefreshTokenCommandIsValidWhenTokenProvided()
        {
            var validator = new RefreshTokenValidator();
            var command = new RefreshTokenCommand("refresh-token");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRefreshTokenCommandIsInvalidWhenTokenMissing()
        {
            var validator = new RefreshTokenValidator();
            var command = new RefreshTokenCommand("");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatLogoutCommandIsValidWhenTokenProvided()
        {
            var validator = new LogoutValidator();
            var command = new LogoutCommand("refresh-token");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatLogoutCommandIsInvalidWhenTokenMissing()
        {
            var validator = new LogoutValidator();
            var command = new LogoutCommand("");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatLoginCommandIsValidWhenEmailHasAllowedDomain()
        {
            var validator = new LoginValidator();
            var command = new LoginCommand { Email = "ahmed@gmail.com", Password = "P@ssw0rd" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatLoginCommandIsValidWhenEmailUsesAnyValidDomain()
        {
            var validator = new LoginValidator();
            var command = new LoginCommand { Email = "admin@furni.test", Password = "P@ssw0rd" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatLoginCommandIsInvalidWhenEmailFormatIsWrong()
        {
            var validator = new LoginValidator();
            var command = new LoginCommand { Email = "not-an-email", Password = "P@ssw0rd" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatForgetPasswordCommandIsValidWhenEmailHasAllowedDomain()
        {
            var validator = new ForgetPasswordValidator();
            var command = new ForgetPasswordCommand("ahmed@yahoo.com");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatForgetPasswordCommandIsInvalidWhenEmailFormatIsWrong()
        {
            var validator = new ForgetPasswordValidator();
            var command = new ForgetPasswordCommand("ahmed@");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendEmailConfirmationCommandIsValidWhenEmailHasAllowedDomain()
        {
            var validator = new SendEmailConfirmationValidator();
            var command = new SendEmailConfirmationCommand("ahmed@hotmail.com");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSendEmailConfirmationCommandIsValidWhenEmailUsesAnyValidDomain()
        {
            var validator = new SendEmailConfirmationValidator();
            var command = new SendEmailConfirmationCommand("ahmed@company.com");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSendOTPCommandIsValidWhenPhoneNumberIsEgyptian()
        {
            var validator = new SendOTPForPhoneVerificationCommandValidator();
            var command = new SendOTPForPhoneVerificationCommand("01012345678");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSendOTPCommandIsInvalidWhenPhoneNumberIsWrongFormat()
        {
            var validator = new SendOTPForPhoneVerificationCommandValidator();
            var command = new SendOTPForPhoneVerificationCommand("01612345678");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatVerifyOTPCommandIsValidWhenOTPHasSixDigits()
        {
            var validator = new VerifyOTPForPhoneVerificationCommandValidator();
            var command = new VerifyOTPForPhoneVerificationCommand("123456");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatVerifyOTPCommandIsInvalidWhenOTPHasWrongFormat()
        {
            var validator = new VerifyOTPForPhoneVerificationCommandValidator();
            var command = new VerifyOTPForPhoneVerificationCommand("12345");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatChangePasswordCommandIsValidWhenPasswordsMatch()
        {
            var validator = new ChangePasswordValidator();
            var command = new ChangePasswordCommand
            {
                OldPassword = "OldP@ss",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "NewP@ss"
            };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatChangePasswordCommandIsInvalidWhenPasswordsDoNotMatch()
        {
            var validator = new ChangePasswordValidator();
            var command = new ChangePasswordCommand
            {
                OldPassword = "OldP@ss",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "Different"
            };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatResetPasswordCommandIsValidWhenAllFieldsProvided()
        {
            var validator = new ResetPasswordValidator();
            var command = new ResetPasswordCommand
            {
                ConfirmationToken = "token",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "NewP@ss"
            };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatResetPasswordCommandIsInvalidWhenTokenMissing()
        {
            var validator = new ResetPasswordValidator();
            var command = new ResetPasswordCommand
            {
                ConfirmationToken = "",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "NewP@ss"
            };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatResetPasswordCommandIsInvalidWhenPasswordsDoNotMatch()
        {
            var validator = new ResetPasswordValidator();
            var command = new ResetPasswordCommand
            {
                ConfirmationToken = "token",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "Different"
            };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRegisterCommandIsValidWhenAllFieldsProvidedAndEmailIsNew()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness("ahmed@gmail.com")).Returns(false);
            var command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Phone = "01012345678",
                Address = "Cairo",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            var result = _registerValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRegisterCommandIsInvalidWhenEmailIsAlreadyUsed()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(true);
            var command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            var result = _registerValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRegisterCommandIsInvalidWhenFirstNameIsTooShort()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new RegisterCommand
            {
                FName = "Ah",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            var result = _registerValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRegisterCommandIsInvalidWhenPhoneIsWrongFormat()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Phone = "01912345678",
                Password = "P@ssw0rd",
                ConfirmPassword = "P@ssw0rd"
            };

            var result = _registerValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRegisterCommandIsInvalidWhenPasswordsDoNotMatch()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new RegisterCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Password = "P@ssw0rd",
                ConfirmPassword = "Different"
            };

            var result = _registerValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatEditProfileCommandIsValidWhenAllFieldsProvidedAndEmailIsNew()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new EditProfileCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com",
                Phone = "01012345678",
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "NewP@ss"
            };

            var result = _editProfileValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatEditProfileCommandIsInvalidWhenEmailIsAlreadyUsed()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(true);
            var command = new EditProfileCommand
            {
                FName = "Ahmed",
                LName = "Hassan",
                Email = "ahmed@gmail.com"
            };

            var result = _editProfileValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatEditProfileCommandIsInvalidWhenPasswordsDoNotMatch()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new EditProfileCommand
            {
                NewPassword = "NewP@ss",
                ConfirmNewPassword = "Different"
            };

            var result = _editProfileValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatEditProfileCommandIsInvalidWhenLastNameIsTooLong()
        {
            _identityService.Setup(s => s.CheckEmailUniqueness(It.IsAny<string>())).Returns(false);
            var command = new EditProfileCommand { LName = new string('a', 31) };

            var result = _editProfileValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
