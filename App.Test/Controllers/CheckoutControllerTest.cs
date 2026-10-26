using App.Application.Checkout.Command.PlaceOrder;
using App.Application.Checkout.Dto;
using App.Application.Checkout.Queries.GetCheckoutDetails;
using App.Application.Responses;
using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Checkout")]
    public class CheckoutControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private CheckoutController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new CheckoutController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatIndexReturnsOkWhenCartHasItems()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCartQuantitiesCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _sender.Setup(s => s.Send(
                    It.IsAny<GetCheckoutDetailsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CheckoutDto
                {
                    CartItems = [new App.Application.ShoppingCarts.Dto.CartItemDto { ProductName = "Chair" }]
                });

            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = []
            };
            var result = await _controller.Index(command);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatIndexReturnsBadRequestWhenCartIsEmpty()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCartQuantitiesCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _sender.Setup(s => s.Send(
                    It.IsAny<GetCheckoutDetailsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CheckoutDto());

            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = []
            };
            var result = await _controller.Index(command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatPlaceOrderReturnsOkWhenOrderPlaced()
        {
            var orderId = Guid.NewGuid();
            _sender.Setup(s => s.Send(
                    It.IsAny<PlaceOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Success(orderId));

            var command = new PlaceOrderCommand { PaymentMethod = "Cash" };
            var result = await _controller.PlaceOrder(command);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatPlaceOrderReturnsBadRequestWhenOrderFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<PlaceOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Failure("The shopping cart is empty."));

            var command = new PlaceOrderCommand { PaymentMethod = "Cash" };
            var result = await _controller.PlaceOrder(command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }
    }
}
