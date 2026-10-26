using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.Application.Auth.Command.VerifyOTPForPhoneVerification
{
    public class VerifyOTPForPhoneVerificationCommandHandler(IConfiguration configuration, IIdentityService identityService, ICurrentUserService currentUserService, IMemoryService<string> memoryService) : IRequestHandler<VerifyOTPForPhoneVerificationCommand, string>
    {
        public async Task<string> Handle(VerifyOTPForPhoneVerificationCommand request, CancellationToken cancellationToken)
        {
            var apiKey = configuration["AUTHEVO_KEY"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var userId = currentUserService.UserId ?? throw new NotFoundException("User not found");
            var requestBody = new
            {
                phone = "+2" + await memoryService.GetObjectAsync($"auth:phone:{userId}"),
                code = request.OTP
            };
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.authevo.dev/v1/otp/verify", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
            if (responseJson.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("verified", out var verified) && verified.GetBoolean())
                {
                    var user = await identityService.GetUserByIdAsync(userId) ?? throw new NotFoundException("User not found");
                    await identityService.ConfirmUserPhoneAsync(user);
                }

            }
            return responseBody;
        }
    }
}
