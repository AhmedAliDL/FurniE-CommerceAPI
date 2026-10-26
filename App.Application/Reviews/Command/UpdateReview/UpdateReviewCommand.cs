using MediatR;

namespace App.Application.Reviews.Command.UpdateReview
{
    public record UpdateReviewCommand : IRequest<bool>
    {
        public Guid ReviewId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
