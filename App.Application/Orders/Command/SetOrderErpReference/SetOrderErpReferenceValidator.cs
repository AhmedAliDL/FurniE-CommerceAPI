using FluentValidation;

namespace App.Application.Orders.Command.SetOrderErpReference
{
    public class SetOrderErpReferenceValidator : AbstractValidator<SetOrderErpReferenceCommand>
    {
        public SetOrderErpReferenceValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");
            RuleFor(c => c.ErpOrderId)
                .MaximumLength(100)
                .When(c => c.ErpOrderId is not null)
                .WithMessage("ERP order id must not exceed 100 characters.");
            RuleFor(c => c.ErpInvoiceId)
                .MaximumLength(100)
                .When(c => c.ErpInvoiceId is not null)
                .WithMessage("ERP invoice id must not exceed 100 characters.");
        }
    }
}
