using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Application.Payments.Dto.Paymob
{
    // Envelope of the Paymob webhook POST body; the HMAC signature arrives as a query string.
    // Types were validated against real Paymob callbacks: amount-like fields are decimal because
    // Paymob serializes the same field as "30" or "30.0" unpredictably; timestamps without a
    // timezone offset and polymorphic values ([] arrays, {} objects) use string?/JsonElement?
    // because System.Text.Json cannot map them to stronger types.
    public sealed record PaymobCallbackDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("obj")]
        public PaymobTransactionDto? Obj { get; init; }

        [JsonPropertyName("accept_fees")]
        public decimal AcceptFees { get; init; }

        [JsonPropertyName("issuer_bank")]
        public string? IssuerBank { get; init; }

        [JsonPropertyName("transaction_processed_callback_responses")]
        public string? TransactionProcessedCallbackResponses { get; init; }
    }

    public sealed record PaymobTransactionDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("pending")]
        public bool Pending { get; init; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; init; }

        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("is_auth")]
        public bool IsAuth { get; init; }

        [JsonPropertyName("is_capture")]
        public bool IsCapture { get; init; }

        [JsonPropertyName("is_standalone_payment")]
        public bool IsStandalonePayment { get; init; }

        [JsonPropertyName("is_voided")]
        public bool IsVoided { get; init; }

        [JsonPropertyName("is_refunded")]
        public bool IsRefunded { get; init; }

        [JsonPropertyName("is_3d_secure")]
        public bool Is3DSecure { get; init; }

        [JsonPropertyName("integration_id")]
        public long IntegrationId { get; init; }

        [JsonPropertyName("profile_id")]
        public long ProfileId { get; init; }

        [JsonPropertyName("has_parent_transaction")]
        public bool HasParentTransaction { get; init; }

        [JsonPropertyName("order")]
        public PaymobOrderDto? Order { get; init; }

        // Kept as string so the HMAC is computed over Paymob's exact value.
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        // Paymob sends this as an array ([]) inside obj but as a string at the envelope level.
        [JsonPropertyName("transaction_processed_callback_responses")]
        public JsonElement? TransactionProcessedCallbackResponses { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("source_data")]
        public PaymobSourceDataDto? SourceData { get; init; }

        [JsonPropertyName("api_source")]
        public string? ApiSource { get; init; }

        [JsonPropertyName("terminal_id")]
        public long? TerminalId { get; init; }

        [JsonPropertyName("merchant_commission")]
        public decimal? MerchantCommission { get; init; }

        [JsonPropertyName("accept_fees")]
        public decimal AcceptFees { get; init; }

        [JsonPropertyName("installment")]
        public string? Installment { get; init; }

        [JsonPropertyName("discount_details")]
        public List<string?>? DiscountDetails { get; init; }

        [JsonPropertyName("is_void")]
        public bool IsVoid { get; init; }

        [JsonPropertyName("is_refund")]
        public bool IsRefund { get; init; }

        [JsonPropertyName("data")]
        public PaymobGatewayDataDto? Data { get; init; }

        [JsonPropertyName("is_hidden")]
        public bool IsHidden { get; init; }

        [JsonPropertyName("payment_key_claims")]
        public PaymobPaymentKeyClaimsDto? PaymentKeyClaims { get; init; }

        [JsonPropertyName("error_occured")]
        public bool ErrorOccurred { get; init; }

        [JsonPropertyName("is_live")]
        public bool IsLive { get; init; }

        [JsonPropertyName("other_endpoint_reference")]
        public string? OtherEndpointReference { get; init; }

        [JsonPropertyName("refunded_amount_cents")]
        public long? RefundedAmountCents { get; init; }

        [JsonPropertyName("source_id")]
        public long? SourceId { get; init; }

        [JsonPropertyName("is_captured")]
        public bool IsCaptured { get; init; }

        [JsonPropertyName("captured_amount")]
        public decimal? CapturedAmount { get; init; }

        [JsonPropertyName("merchant_staff_tag")]
        public string? MerchantStaffTag { get; init; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; init; }

        [JsonPropertyName("is_settled")]
        public bool IsSettled { get; init; }

        [JsonPropertyName("bill_balanced")]
        public bool BillBalanced { get; init; }

        [JsonPropertyName("is_bill")]
        public bool IsBill { get; init; }

        [JsonPropertyName("owner")]
        public long? Owner { get; init; }

        [JsonPropertyName("parent_transaction")]
        public string? ParentTransaction { get; init; }
    }

    public sealed record PaymobOrderItem
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; init; }

        [JsonPropertyName("quantity")]
        public long Quantity { get; init; }
    }

    public sealed record PaymobOrderDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        [JsonPropertyName("delivery_needed")]
        public bool DeliveryNeeded { get; init; }

        [JsonPropertyName("merchant")]
        public PaymobMerchantDto? Merchant { get; init; }

        [JsonPropertyName("collector")]
        public string? Collector { get; init; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; init; }

        [JsonPropertyName("shipping_data")]
        public PaymobShippingDataDto? ShippingData { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("is_payment_locked")]
        public bool IsPaymentLocked { get; init; }

        [JsonPropertyName("is_return")]
        public bool IsReturn { get; init; }

        [JsonPropertyName("is_cancel")]
        public bool IsCancel { get; init; }

        [JsonPropertyName("is_returned")]
        public bool IsReturned { get; init; }

        [JsonPropertyName("is_canceled")]
        public bool IsCanceled { get; init; }

        // Carries back the special_reference sent with the intention (the local OrderId).
        [JsonPropertyName("merchant_order_id")]
        public string? MerchantOrderId { get; init; }

        [JsonPropertyName("wallet_notification")]
        public string? WalletNotification { get; init; }

        [JsonPropertyName("paid_amount_cents")]
        public long PaidAmountCents { get; init; }

        [JsonPropertyName("notify_user_with_email")]
        public bool NotifyUserWithEmail { get; init; }

        [JsonPropertyName("items")]
        public List<PaymobOrderItem>? Items { get; init; }

        [JsonPropertyName("order_url")]
        public string? OrderUrl { get; init; }

        [JsonPropertyName("commission_fees")]
        public decimal? CommissionFees { get; init; }

        [JsonPropertyName("delivery_fees_cents")]
        public long? DeliveryFeesCents { get; init; }

        [JsonPropertyName("delivery_vat_cents")]
        public long? DeliveryVatCents { get; init; }

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; init; }

        [JsonPropertyName("merchant_staff_tag")]
        public string? MerchantStaffTag { get; init; }

        [JsonPropertyName("api_source")]
        public string? ApiSource { get; init; }

        [JsonPropertyName("data")]
        public PaymobOrderData? Data { get; init; }

        [JsonPropertyName("payment_status")]
        public string? PaymentStatus { get; init; }

        [JsonPropertyName("terminal_version")]
        public string? TerminalVersion { get; init; }

        [JsonPropertyName("payme_details")]
        public string? PaymeDetails { get; init; }
    }

    public sealed record PaymobOrderData
    {
        [JsonPropertyName("notification_url")]
        public string NotificationUrl { get; init; } = string.Empty;
    }

    public sealed record PaymobMerchantDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        [JsonPropertyName("phones")]
        public List<string>? Phones { get; init; }

        [JsonPropertyName("company_emails")]
        public List<string>? CompanyEmails { get; init; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; init; }

        [JsonPropertyName("street")]
        public string? Street { get; init; }
    }

    public sealed record PaymobShippingDataDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("street")]
        public string? Street { get; init; }

        [JsonPropertyName("building")]
        public string? Building { get; init; }

        [JsonPropertyName("floor")]
        public string? Floor { get; init; }

        [JsonPropertyName("apartment")]
        public string? Apartment { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; init; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; init; }

        [JsonPropertyName("extra_description")]
        public string? ExtraDescription { get; init; }

        [JsonPropertyName("shipping_method")]
        public string? ShippingMethod { get; init; }

        [JsonPropertyName("order_id")]
        public long OrderId { get; init; }

        [JsonPropertyName("order")]
        public long Order { get; init; }
    }

    public sealed record PaymobSourceDataDto
    {
        [JsonPropertyName("pan")]
        public string? Pan { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("tenure")]
        public string? Tenure { get; init; }

        [JsonPropertyName("sub_type")]
        public string? SubType { get; init; }
    }

    public sealed record PaymobGatewayDataDto
    {
        [JsonPropertyName("gateway_integration_pk")]
        public long GatewayIntegrationPk { get; init; }

        [JsonPropertyName("klass")]
        public string? Klass { get; init; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("migs_order")]
        public PaymobMigsOrderDto? MigsOrder { get; init; }

        [JsonPropertyName("merchant")]
        public string? Merchant { get; init; }

        [JsonPropertyName("migs_result")]
        public string? MigsResult { get; init; }

        [JsonPropertyName("migs_transaction")]
        public PaymobMigsTransactionDto? MigsTransaction { get; init; }

        [JsonPropertyName("txn_response_code")]
        public string? TxnResponseCode { get; init; }

        [JsonPropertyName("acq_response_code")]
        public string? AcqResponseCode { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("merchant_txn_ref")]
        public string? MerchantTxnRef { get; init; }

        [JsonPropertyName("order_info")]
        public string? OrderInfo { get; init; }

        [JsonPropertyName("receipt_no")]
        public string? ReceiptNo { get; init; }

        [JsonPropertyName("transaction_no")]
        public string? TransactionNo { get; init; }

        [JsonPropertyName("batch_no")]
        public long? BatchNo { get; init; }

        [JsonPropertyName("authorize_id")]
        public string? AuthorizeId { get; init; }

        [JsonPropertyName("card_type")]
        public string? CardType { get; init; }

        [JsonPropertyName("card_num")]
        public string? CardNum { get; init; }

        [JsonPropertyName("secure_hash")]
        public string? SecureHash { get; init; }

        [JsonPropertyName("avs_result_code")]
        public string? AvsResultCode { get; init; }

        [JsonPropertyName("avs_acq_response_code")]
        public string? AvsAcqResponseCode { get; init; }

        [JsonPropertyName("captured_amount")]
        public decimal? CapturedAmount { get; init; }

        [JsonPropertyName("authorised_amount")]
        public decimal? AuthorisedAmount { get; init; }

        [JsonPropertyName("refunded_amount")]
        public decimal? RefundedAmount { get; init; }

        [JsonPropertyName("acs_eci")]
        public string? AcsEci { get; init; }
    }

    public sealed record PaymobMigsOrderDto
    {
        [JsonPropertyName("acceptPartialAmount")]
        public bool AcceptPartialAmount { get; init; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }

        [JsonPropertyName("authenticationStatus")]
        public string? AuthenticationStatus { get; init; }

        [JsonPropertyName("chargeback")]
        public PaymobChargebackDto? Chargeback { get; init; }

        [JsonPropertyName("creationTime")]
        public DateTimeOffset? CreationTime { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("lastUpdatedTime")]
        public DateTimeOffset? LastUpdatedTime { get; init; }

        [JsonPropertyName("merchantAmount")]
        public decimal MerchantAmount { get; init; }

        [JsonPropertyName("merchantCategoryCode")]
        public string? MerchantCategoryCode { get; init; }

        [JsonPropertyName("merchantCurrency")]
        public string? MerchantCurrency { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("totalAuthorizedAmount")]
        public decimal TotalAuthorizedAmount { get; init; }

        [JsonPropertyName("totalCapturedAmount")]
        public decimal TotalCapturedAmount { get; init; }

        [JsonPropertyName("totalRefundedAmount")]
        public decimal? TotalRefundedAmount { get; init; }
    }

    public sealed record PaymobChargebackDto
    {
        [JsonPropertyName("amount")]
        public decimal? Amount { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }
    }

    public sealed record PaymobMigsTransactionDto
    {
        [JsonPropertyName("acquirer")]
        public PaymobAcquirerDto? Acquirer { get; init; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }

        [JsonPropertyName("authenticationStatus")]
        public string? AuthenticationStatus { get; init; }

        [JsonPropertyName("authorizationCode")]
        public string? AuthorizationCode { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("receipt")]
        public string? Receipt { get; init; }

        [JsonPropertyName("source")]
        public string? Source { get; init; }

        [JsonPropertyName("stan")]
        public string? Stan { get; init; }

        [JsonPropertyName("terminal")]
        public string? Terminal { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }
    }

    public sealed record PaymobAcquirerDto
    {
        [JsonPropertyName("batch")]
        public long? Batch { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("merchantId")]
        public string? MerchantId { get; init; }

        [JsonPropertyName("settlementDate")]
        public string? SettlementDate { get; init; }

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; init; }

        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; init; }
    }

    public sealed record PaymobPaymentKeyClaimsDto
    {
        [JsonPropertyName("extra")]
        public JsonElement? Extra { get; init; }

        [JsonPropertyName("user_id")]
        public long UserId { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("order_id")]
        public long OrderId { get; init; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; init; }

        [JsonPropertyName("billing_data")]
        public PaymobBillingDataDto? BillingData { get; init; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; init; }

        [JsonPropertyName("integration_id")]
        public long IntegrationId { get; init; }

        [JsonPropertyName("lock_order_when_paid")]
        public bool LockOrderWhenPaid { get; init; }

        [JsonPropertyName("next_payment_intention")]
        public string? NextPaymentIntention { get; init; }

        [JsonPropertyName("single_payment_attempt")]
        public bool SinglePaymentAttempt { get; init; }
    }

    public sealed record PaymobBillingDataDto
    {
        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("floor")]
        public string? Floor { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("street")]
        public string? Street { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("building")]
        public string? Building { get; init; }

        [JsonPropertyName("apartment")]
        public string? Apartment { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; init; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; init; }

        [JsonPropertyName("extra_description")]
        public string? ExtraDescription { get; init; }
    }
}
