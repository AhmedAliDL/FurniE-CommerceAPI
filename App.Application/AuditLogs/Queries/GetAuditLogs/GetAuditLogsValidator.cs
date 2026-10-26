using FluentValidation;

namespace App.Application.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsValidator : AbstractValidator<GetAuditLogsQuery>
    {
        public GetAuditLogsValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");
            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
            RuleFor(q => q.EntityName)
                .MaximumLength(200)
                .When(q => q.EntityName is not null)
                .WithMessage("Entity name must not exceed 200 characters.");
        }
    }
}
