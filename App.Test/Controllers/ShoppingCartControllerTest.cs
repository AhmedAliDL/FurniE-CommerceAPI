using App.Application.ShoppingCarts.Command.AddCartItem;
using App.Application.ShoppingCarts.Command.ClearCart;
using App.Application.ShoppingCarts.Command.DeleteCartItem;
using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using App.Application.ShoppingCarts.Dto;
using App.Application.ShoppingCarts.Queries.GetCartItems;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("ShoppingCart")]
    public class ShoppingCartControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private ShoppingCartController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new ShoppingCartController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatIndexReturnsOkWhenCartExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetCartItemsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCartDto());

            var result = await _controller.Index();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatClearCartReturnsNoContentWhenCartCleared()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ClearCartCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ClearCart();

            Assert.That(result, Is.TypeOf<NoContentResult>());
            _sender.Verify(s => s.Send(It.IsAny<ClearCartCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureThatAddItemReturnsOkWhenProductAdded()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<AddCartItemCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<string>.Success("Product added to your cart."));

            var result = await _controller.AddItem(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatAddItemReturnsBadRequestWhenProductNotAdded()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<AddCartItemCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<string>.Failure("Product not found."));

            var result = await _controller.AddItem(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatUpdateQuantityReturnsNoContentWhenQuantitiesUpdated()
        {
            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = [new CartItemQuantity { ProductId = Guid.NewGuid(), Quantity = 2 }]
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCartQuantitiesCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateQuantity(command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteProductReturnsNoContentWhenItemDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteCartItemCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteProduct(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }
    }
}
