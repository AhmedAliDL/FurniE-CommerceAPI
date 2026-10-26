using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using App.Application.Common.Interfaces.Services;
using App.Application.Payments.Dto.Paymob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services
{
    public class PaymobService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PaymobService> logger) : IPaymobService
    {
        private static readonly JsonSerializerOptions IntentionJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task<PaymobIntentionResponse> CreateIntentionAsync(PaymobIntentionRequest request, CancellationToken cancellationToken = default)
        {
            var client = httpClientFactory.CreateClient(nameof(PaymobService));
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{configuration["Paymob:BaseUrl"]!.TrimEnd('/')}/v1/intention/")
            {
                Content = new StringContent(JsonSerializer.Serialize(request, IntentionJsonOptions), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Token", configuration["Paymob:SecretKey"]);

            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Paymob intention request failed. {StatusCode} {Body}", (int)response.StatusCode, content);
                throw new HttpRequestException("The payment session could not be created. Please try again later.");
            }

            return JsonSerializer.Deserialize<PaymobIntentionResponse>(content, IntentionJsonOptions)
                ?? throw new InvalidOperationException("Paymob returned an empty intention response.");
        }

        public string BuildCheckoutUrl(string clientSecret) =>
            $"{configuration["Paymob:CheckoutBaseUrl"]!.TrimEnd('/')}/?publicKey=" +
            $"{Uri.EscapeDataString(configuration["Paymob:PublicKey"]!)}&clientSecret={Uri.EscapeDataString(clientSecret)}";

        public async Task<PaymobOperationResult> RefundTransactionAsync(long transactionId, long amountCents, CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            return await SendVoidRefundAsync("refund", token, transactionId, amountCents, cancellationToken);
        }

        public async Task<PaymobOperationResult> VoidTransactionAsync(long transactionId, CancellationToken cancellationToken = default)
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            return await SendVoidRefundAsync("void", token, transactionId, null, cancellationToken);
        }

        private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient(nameof(PaymobService));
            var request = new Dictionary<string, string> { ["api_key"] = configuration["Paymob:ApiKey"]! };
            using var response = await client.PostAsJsonAsync(
                $"{configuration["Paymob:BaseUrl"]!.TrimEnd('/')}/api/auth/tokens",
                request,
                cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Paymob auth token request failed. {StatusCode} {Body}", (int)response.StatusCode, content);
                throw new HttpRequestException("Could not authenticate with Paymob. Please try again later.");
            }

            return JsonSerializer.Deserialize<PaymobAuthTokenResponse>(content)?.Token
                ?? throw new HttpRequestException("Could not authenticate with Paymob. Please try again later.");
        }

        private async Task<PaymobOperationResult> SendVoidRefundAsync(string operation, string token, long transactionId, long? amountCents, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                ["auth_token"] = token,
                ["transaction_id"] = transactionId
            };
            if (amountCents.HasValue)
                body["amount_cents"] = amountCents.Value;

            var client = httpClientFactory.CreateClient(nameof(PaymobService));
            // Paymob routes void/refund without a trailing slash; the slash variant returns 404.
            var url = $"{configuration["Paymob:BaseUrl"]!.TrimEnd('/')}/api/acceptance/void_refund/{operation}";
            using var response = await client.PostAsJsonAsync(url, body, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                logger.LogError("Paymob {Operation} returned an empty response. TransactionId {TransactionId} StatusCode {StatusCode}",
                    operation, transactionId, (int)response.StatusCode);
                return new PaymobOperationResult
                {
                    Success = false,
                    Message = $"Paymob returned an empty response (HTTP {(int)response.StatusCode})."
                };
            }

            PaymobVoidRefundResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PaymobVoidRefundResponse>(content);
            }
            catch (JsonException)
            {
                logger.LogError("Paymob {Operation} returned an unreadable response. TransactionId {TransactionId} StatusCode {StatusCode} Body {Body}",
                    operation, transactionId, (int)response.StatusCode, content);
                return new PaymobOperationResult
                {
                    Success = false,
                    Message = $"Paymob returned an unexpected response (HTTP {(int)response.StatusCode})."
                };
            }
            // Paymob answers with 200 even for business errors (e.g. refundable limit exceeded),
            // so success is read from the payload, not the status code. A processed refund/void
            // returns the new transaction at the root (success/is_refund), other shapes wrap it.
            var succeeded = (payload?.Transaction is { } txn && txn.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                || payload?.Success == true
                || payload?.Message?.Contains("success", StringComparison.OrdinalIgnoreCase) == true;
            if (!succeeded)
            {
                logger.LogWarning("Paymob {Operation} failed. TransactionId {TransactionId} {Body}",
                    operation, transactionId, content);
            }

            return new PaymobOperationResult
            {
                Success = succeeded,
                Message = payload?.Message ?? payload?.Data?.Message
            };
        }

        public bool VerifyTransactionHmac(PaymobTransactionDto transaction, string receivedHmac)
        {
            if (string.IsNullOrEmpty(receivedHmac))
                return false;

            // Field order fixed by Paymob's documented transaction-callback HMAC recipe.
            var data = string.Concat(
                transaction.AmountCents,
                transaction.CreatedAt,
                transaction.Currency,
                transaction.ErrorOccurred.ToString().ToLower(),
                transaction.HasParentTransaction.ToString().ToLower(),
                transaction.Id,
                transaction.IntegrationId,
                transaction.Is3DSecure.ToString().ToLower(),
                transaction.IsAuth.ToString().ToLower(),
                transaction.IsCapture.ToString().ToLower(),
                transaction.IsRefunded.ToString().ToLower(),
                transaction.IsStandalonePayment.ToString().ToLower(),
                transaction.IsVoided.ToString().ToLower(),
                transaction.Order?.Id,
                transaction.Owner,
                transaction.Pending.ToString().ToLower(),
                transaction.SourceData?.Pan,
                transaction.SourceData?.SubType,
                transaction.SourceData?.Type,
                transaction.Success.ToString().ToLower());

            var computedHmac = Convert.ToHexString(HMACSHA512.HashData(
                Encoding.UTF8.GetBytes(configuration["Paymob:HmacSecret"]!),
                Encoding.UTF8.GetBytes(data)));
            var valid = string.Equals(computedHmac, receivedHmac, StringComparison.OrdinalIgnoreCase);
            if (!valid)
                logger.LogWarning("Paymob callback HMAC verification failed. TransactionId {TransactionId}", transaction.Id);
            return valid;
        }
    }
}
