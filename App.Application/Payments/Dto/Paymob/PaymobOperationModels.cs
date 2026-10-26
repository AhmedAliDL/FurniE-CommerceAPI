using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Application.Payments.Dto.Paymob
{
    // Result of a refund/void call; Paymob reports business errors inside a 200 body,
    // so Success is derived from the payload, not the status code.
    public sealed record PaymobOperationResult
    {
        public bool Success { get; init; }

        public string? Message { get; init; }
    }

    public sealed record PaymobVoidRefundResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("transaction")]
        public JsonElement? Transaction { get; init; }

        // Successful refunds/voids return the new transaction at the root of the body.
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        [JsonPropertyName("data")]
        public PaymobVoidRefundData? Data { get; init; }
    }

    public sealed record PaymobVoidRefundData
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    public sealed record PaymobAuthTokenResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }
}
