using System.Text.Json.Serialization;

namespace App.Application.Payments.Dto.Paymob
{
    // Serialized with a snake_case naming policy by PaymobService.
    public sealed record PaymobIntentionRequest
    {
        // The Intention API uses "amount" (in cents), not "amount_cents".
        [JsonPropertyName("amount")]
        public long AmountCents { get; init; }
        public string Currency { get; init; } = "EGP";
        public List<int> PaymentMethods { get; init; } = [];
        public List<PaymobIntentionItem> Items { get; init; } = [];
        public PaymobBillingData BillingData { get; init; } = new();
        public string? SpecialReference { get; init; }
        public int Expiration { get; init; }
        public string? NotificationUrl { get; init; }
        public string? RedirectionUrl { get; init; }
    }

    public sealed record PaymobIntentionItem
    {
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("amount")]
        public long AmountCents { get; init; }
        public string? Description { get; init; }
        public int Quantity { get; init; }
    }

    // Paymob requires every billing field; send "NA" when unknown.
    public sealed record PaymobBillingData
    {
        public string FirstName { get; init; } = "NA";
        public string LastName { get; init; } = "NA";
        public string Email { get; init; } = "NA";
        public string PhoneNumber { get; init; } = "NA";
        public string Apartment { get; init; } = "NA";
        public string Floor { get; init; } = "NA";
        public string Street { get; init; } = "NA";
        public string Building { get; init; } = "NA";
        public string City { get; init; } = "NA";
        public string Country { get; init; } = "NA";
        public string State { get; init; } = "NA";
    }

    public sealed record PaymobIntentionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("intention_order_id")]
        public long IntentionOrderId { get; init; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; init; } = string.Empty;

        [JsonPropertyName("special_reference")]
        public string? SpecialReference { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("created")]
        public DateTime Created { get; init; }

        [JsonPropertyName("confirmed")]
        public bool Confirmed { get; init; }

        [JsonPropertyName("payment_methods")]
        public List<PaymobPaymentMethodDto> PaymentMethods { get; init; } = [];
    }

    public sealed record PaymobPaymentMethodDto
    {
        [JsonPropertyName("integration_id")]
        public int IntegrationId { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("method_type")]
        public string MethodType { get; init; } = string.Empty;

        [JsonPropertyName("live")]
        public bool Live { get; init; }
    }
}
