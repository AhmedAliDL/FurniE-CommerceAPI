using App.Application.Orders.Command.AcceptReturnRequest;
using App.Application.Orders.Command.CancelOrder;
using App.Application.Orders.Command.ProcessReturnRefund;
using App.Application.Orders.Command.RejectCancellationRequest;
using App.Application.Orders.Command.RejectReturnRequest;
using App.Application.Orders.Command.RequestOrderCancellation;
using App.Application.Orders.Command.RequestOrderReturn;
using App.Application.Orders.Command.UpdateOrderStatus;
using App.Application.Orders.Dto;
using App.Application.Orders.Queries.GetCancellationRequests;
using App.Application.Orders.Queries.GetReturnRequests;
using App.Application.Orders.Queries.GetUserOrders;
using App.Domain.Enums;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Orders")]
    public class OrdersValidatorsTest
    {
        [Test]
        public void EnsureThatUpdateOrderStatusCommandIsValidWhenFieldsProvided()
        {
            var validator = new UpdateOrderStatusValidator();
            var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateOrderStatusCommandIsInvalidWhenOrderIdMissing()
        {
            var validator = new UpdateOrderStatusValidator();
            var command = new UpdateOrderStatusCommand(Guid.Empty, OrderStatus.Confirmed);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateOrderStatusCommandIsInvalidWhenStatusNotInEnum()
        {
            var validator = new UpdateOrderStatusValidator();
            var command = new UpdateOrderStatusCommand(Guid.NewGuid(), (OrderStatus)99);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCancelOrderCommandIsValidWhenOrderIdProvided()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCancelOrderCommandIsInvalidWhenOrderIdMissing()
        {
            var validator = new CancelOrderValidator();
            var command = new CancelOrderCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRequestOrderCancellationCommandIsValidWhenFieldsProvided()
        {
            var validator = new RequestOrderCancellationValidator();
            var command = new RequestOrderCancellationCommand(Guid.NewGuid(), "Found a better price");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRequestOrderCancellationCommandIsInvalidWhenReasonMissing()
        {
            var validator = new RequestOrderCancellationValidator();
            var command = new RequestOrderCancellationCommand(Guid.NewGuid(), "");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRequestOrderCancellationCommandIsInvalidWhenReasonTooLong()
        {
            var validator = new RequestOrderCancellationValidator();
            var command = new RequestOrderCancellationCommand(Guid.NewGuid(), new string('a', 501));

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRejectCancellationRequestCommandIsValidWhenFieldsProvided()
        {
            var validator = new RejectCancellationRequestValidator();
            var command = new RejectCancellationRequestCommand(Guid.NewGuid(), "Order is already packed");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRejectCancellationRequestCommandIsInvalidWhenNoteMissing()
        {
            var validator = new RejectCancellationRequestValidator();
            var command = new RejectCancellationRequestCommand(Guid.NewGuid(), "");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRequestOrderReturnCommandIsValidWhenItemsProvided()
        {
            var validator = new RequestOrderReturnValidator();
            var command = new RequestOrderReturnCommand(
                Guid.NewGuid(),
                "MerchantDefect",
                "Chair leg arrived broken",
                [new ReturnItemRequestDto { OrderItemId = Guid.NewGuid(), Quantity = 1 }]);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRequestOrderReturnCommandIsInvalidWhenReasonCategoryIsInvalid()
        {
            var validator = new RequestOrderReturnValidator();
            var command = new RequestOrderReturnCommand(
                Guid.NewGuid(),
                "Other",
                "Chair leg arrived broken",
                [new ReturnItemRequestDto { OrderItemId = Guid.NewGuid(), Quantity = 1 }]);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRequestOrderReturnCommandIsInvalidWhenItemsMissing()
        {
            var validator = new RequestOrderReturnValidator();
            var command = new RequestOrderReturnCommand(Guid.NewGuid(), "MerchantDefect", "Chair leg arrived broken", []);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRequestOrderReturnCommandIsInvalidWhenItemQuantityIsNotPositive()
        {
            var validator = new RequestOrderReturnValidator();
            var command = new RequestOrderReturnCommand(
                Guid.NewGuid(),
                "MerchantDefect",
                "Chair leg arrived broken",
                [new ReturnItemRequestDto { OrderItemId = Guid.NewGuid(), Quantity = 0 }]);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAcceptReturnRequestCommandIsValidWhenFieldsProvided()
        {
            var validator = new AcceptReturnRequestValidator();
            var command = new AcceptReturnRequestCommand(Guid.NewGuid(), "Aramex", "01001234567");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatAcceptReturnRequestCommandIsInvalidWhenCourierNameMissing()
        {
            var validator = new AcceptReturnRequestValidator();
            var command = new AcceptReturnRequestCommand(Guid.NewGuid(), "", "01001234567");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRejectReturnRequestCommandIsValidWhenFieldsProvided()
        {
            var validator = new RejectReturnRequestValidator();
            var command = new RejectReturnRequestCommand(Guid.NewGuid(), "Items were used");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRejectReturnRequestCommandIsInvalidWhenNoteMissing()
        {
            var validator = new RejectReturnRequestValidator();
            var command = new RejectReturnRequestCommand(Guid.NewGuid(), "");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatProcessReturnRefundCommandIsValidWhenAmountOmitted()
        {
            var validator = new ProcessReturnRefundValidator();
            var command = new ProcessReturnRefundCommand(Guid.NewGuid(), null);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatProcessReturnRefundCommandIsValidWhenAmountProvided()
        {
            var validator = new ProcessReturnRefundValidator();
            var command = new ProcessReturnRefundCommand(Guid.NewGuid(), 5000);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatProcessReturnRefundCommandIsInvalidWhenAmountIsZero()
        {
            var validator = new ProcessReturnRefundValidator();
            var command = new ProcessReturnRefundCommand(Guid.NewGuid(), 0);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetUserOrdersQueryIsValidWithDefaultValues()
        {
            var validator = new GetUserOrdersValidator();
            var query = new GetUserOrdersQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetUserOrdersQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetUserOrdersValidator();
            var query = new GetUserOrdersQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetCancellationRequestsQueryIsValidWithDefaultValues()
        {
            var validator = new GetCancellationRequestsValidator();
            var query = new GetCancellationRequestsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetCancellationRequestsQueryIsInvalidWhenStatusNotInEnum()
        {
            var validator = new GetCancellationRequestsValidator();
            var query = new GetCancellationRequestsQuery((CancellationRequestStatus)99);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetCancellationRequestsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetCancellationRequestsValidator();
            var query = new GetCancellationRequestsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetReturnRequestsQueryIsValidWithDefaultValues()
        {
            var validator = new GetReturnRequestsValidator();
            var query = new GetReturnRequestsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetReturnRequestsQueryIsInvalidWhenStatusNotInEnum()
        {
            var validator = new GetReturnRequestsValidator();
            var query = new GetReturnRequestsQuery((ReturnRequestStatus)99);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetReturnRequestsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetReturnRequestsValidator();
            var query = new GetReturnRequestsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
