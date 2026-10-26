using App.Application.Responses;
using App.Application.Wishlists.Command.AddToWishlist;
using App.Application.Wishlists.Command.MoveWishlistItemToCart;
using App.Application.Wishlists.Command.RemoveFromWishlist;
using App.Application.Wishlists.Dto;
using App.Application.Wishlists.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Wishlist")]
    public class WishlistControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private WishlistController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new WishlistController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetWishlistReturnsOkWhenWishlistExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetWishlistQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<WishlistItemDto>());

            var result = await _controller.GetWishlist();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatAddToWishlistReturnsNoContentWhenProductAdded()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<AddToWishlistCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddToWishlist(Guid.NewGuid(), 150m);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatMoveToCartReturnsOkWhenItemMoved()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<MoveWishlistItemToCartCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<string>.Success("Item moved to cart"));

            var result = await _controller.MoveToCart(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatMoveToCartReturnsBadRequestWhenItemNotMoved()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<MoveWishlistItemToCartCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<string>.Failure("Product out of stock"));

            var result = await _controller.MoveToCart(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRemoveFromWishlistReturnsNoContentWhenProductRemoved()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<RemoveFromWishlistCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.RemoveFromWishlist(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }
    }
}
