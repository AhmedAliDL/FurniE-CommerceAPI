using FluentValidation;

namespace App.Application.Products.Command.ReorderProductImages
{
    public class ReorderProductImagesValidator : AbstractValidator<ReorderProductImagesCommand>
    {
        public ReorderProductImagesValidator()
        {
            RuleFor(c => c.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");

            RuleFor(c => c.ImageIds)
                .NotEmpty()
                .WithMessage("At least one image id is required.");

            RuleFor(c => c.ImageIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Image ids must not contain duplicates.");
        }
    }
}
