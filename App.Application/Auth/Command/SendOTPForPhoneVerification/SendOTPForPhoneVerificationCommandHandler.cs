using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.Application.Auth.Command.SendOTPForPhoneVerification
{
    public class SendOTPForPhoneVerificationCommandHandler(IConfiguration configuration, IMemoryService<string> memoryService, ICurrentUserService currentUserService) : IRequestHandler<SendOTPForPhoneVerificationCommand, string>
    {
        public async Task<string> Handle(SendOTPForPhoneVerificationCommand request, CancellationToken cancellationToken)
        {
            var apiKey = configuration["AUTHEVO_KEY"];
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            var requestBody = new
            {
                phone = "+2" + request.PhoneNumber
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync("https://api.authevo.dev/v1/otp/send", content
            , cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"OTP request failed: " +
                    $"{(int)response.StatusCode} - {responseBody}"
                );
            }
            var userId = currentUserService.UserId ?? throw new NotFoundException("User not found");

            await memoryService.SetObjectAsync($"auth:phone:{userId}", request.PhoneNumber, TimeSpan.FromMinutes(12));

            return responseBody;
        }
    }
}
