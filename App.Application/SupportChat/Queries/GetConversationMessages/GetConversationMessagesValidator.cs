using FluentValidation;

namespace App.Application.SupportChat.Queries.GetConversationMessages
{
    public class GetConversationMessagesValidator : AbstractValidator<GetConversationMessagesQuery>
    {
        public GetConversationMessagesValidator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
