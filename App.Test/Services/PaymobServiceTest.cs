using System.Net;
using System.Text;
using App.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Services
{
    [TestFixture]
    [Category("Payments")]
    public class PaymobServiceTest
    {
        private IConfiguration _configuration = null!;

        [SetUp]
        public void Setup()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Paymob:BaseUrl"] = "https://paymob.test",
                    ["Paymob:CheckoutBaseUrl"] = "https://checkout.test",
                    ["Paymob:ApiKey"] = "test-api-key",
                    ["Paymob:SecretKey"] = "test-secret-key",
                    ["Paymob:PublicKey"] = "test-public-key",
                    ["Paymob:HmacSecret"] = "test-hmac-secret"
                })
                .Build();
        }

        [Test]
        public async Task EnsureRefundReturnsFailureWhenPaymobReturnsEmptyBody()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, """{"token":"auth-token"}""")
                : Empty(HttpStatusCode.OK));

            var result = await service.RefundTransactionAsync(560876045, 100);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("empty response"));
        }

        [Test]
        public async Task EnsureRefundReturnsFailureWhenPaymobReturnsNonJsonBody()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, """{"token":"auth-token"}""")
                : Json(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>"));

            var result = await service.RefundTransactionAsync(560876045, 100);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("unexpected response"));
        }

        [Test]
        public async Task EnsureRefundSucceedsWhenPaymobReturnsTransaction()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, """{"token":"auth-token"}""")
                : Json(HttpStatusCode.OK, """{"message":"Refunded successfully","transaction":{"id":560876045}}"""));

            var result = await service.RefundTransactionAsync(560876045, 100);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Refunded successfully"));
        }

        [Test]
        public async Task EnsureRefundSucceedsWhenPaymobReturnsRootTransaction()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, """{"token":"auth-token"}""")
                : Json(HttpStatusCode.OK, """{"id":555123457,"pending":false,"amount_cents":100,"success":true,"is_refund":true,"is_void":false}"""));

            var result = await service.RefundTransactionAsync(560876045, 100);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task EnsureRefundReturnsPaymobErrorMessage()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, """{"token":"auth-token"}""")
                : Json(HttpStatusCode.OK, """{"success":false,"data":{"message":"Refund amount exceeds the refundable amount."}}"""));

            var result = await service.RefundTransactionAsync(560876045, 100);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Refund amount exceeds the refundable amount."));
        }

        [Test]
        public void EnsureRefundThrowsHttpRequestExceptionWhenAuthTokenRequestFails()
        {
            var service = CreateService(_ => Json(HttpStatusCode.Unauthorized, """{"detail":"bad api key"}"""));

            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await service.RefundTransactionAsync(560876045, 100));
        }

        [Test]
        public void EnsureRefundThrowsHttpRequestExceptionWhenAuthTokenIsMissing()
        {
            var service = CreateService(request => request.RequestUri!.AbsolutePath == "/api/auth/tokens"
                ? Json(HttpStatusCode.OK, "{}")
                : Json(HttpStatusCode.OK, """{"transaction":{"id":560876045}}"""));

            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await service.RefundTransactionAsync(560876045, 100));
        }

        [Test]
        public async Task EnsureRefundPostsTransactionIdAndAmountToRefundEndpoint()
        {
            string? body = null;
            string? path = null;
            var service = CreateService(request =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/auth/tokens")
                    return Json(HttpStatusCode.OK, """{"token":"auth-token"}""");

                path = request.RequestUri.AbsolutePath;
                body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Json(HttpStatusCode.OK, """{"transaction":{"id":560876045}}""");
            });

            await service.RefundTransactionAsync(560876045, 250);

            Assert.That(path, Is.EqualTo("/api/acceptance/void_refund/refund"));
            Assert.That(body, Does.Contain("\"transaction_id\":560876045"));
            Assert.That(body, Does.Contain("\"amount_cents\":250"));
        }

        [Test]
        public async Task EnsureVoidPostsTransactionIdToVoidEndpointWithoutTrailingSlash()
        {
            string? body = null;
            string? path = null;
            var service = CreateService(request =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/auth/tokens")
                    return Json(HttpStatusCode.OK, """{"token":"auth-token"}""");

                path = request.RequestUri.AbsolutePath;
                body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Json(HttpStatusCode.OK, """{"transaction":{"id":560876045}}""");
            });

            await service.VoidTransactionAsync(560876045);

            Assert.That(path, Is.EqualTo("/api/acceptance/void_refund/void"));
            Assert.That(body, Does.Contain("\"transaction_id\":560876045"));
            Assert.That(body, Does.Not.Contain("amount_cents"));
        }

        private PaymobService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
            new(new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(handler))),
                _configuration,
                new Mock<ILogger<PaymobService>>().Object);

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private static HttpResponseMessage Empty(HttpStatusCode status) =>
            new(status) { Content = new StringContent(string.Empty, Encoding.UTF8, "application/json") };

        private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(handler(request));
        }

        private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => client;
        }
    }
}
