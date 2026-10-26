using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace App.Application.Products.Command.UploadProductImages
{
    public class UploadProductImagesValidator : AbstractValidator<UploadProductImagesCommand>
    {
        public UploadProductImagesValidator(IConfiguration configuration)
        {
            var allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                ?? [".jpg", ".jpeg", ".png", ".webp"];
            var maxSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 5 * 1024 * 1024);
            var maxFiles = configuration.GetValue<int>("FileStorage:MaxFilesPerUpload", 10);

            RuleFor(c => c.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");

            RuleFor(c => c.Files)
                .NotEmpty()
                .WithMessage("At least one image file is required.");

            RuleFor(c => c.Files.Count)
                .LessThanOrEqualTo(maxFiles)
                .WithMessage($"At most {maxFiles} images can be uploaded at once.");

            RuleForEach(c => c.Files).ChildRules(file =>
            {
                file.RuleFor(f => f.Content)
                    .NotNull()
                    .WithMessage("File content is required.");

                file.RuleFor(f => f.Length)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(maxSizeBytes)
                    .WithMessage($"Each image must be between 1 byte and {maxSizeBytes / 1024 / 1024} MB.");

                file.RuleFor(f => f.FileName)
                    .Must(name => allowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                    .WithMessage($"Only the following image types are allowed: {string.Join(", ", allowedExtensions)}.");
            });
        }
    }
}
