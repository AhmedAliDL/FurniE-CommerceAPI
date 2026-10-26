using MediatR;

namespace App.Application.Reviews.Command.AddReview
{
    public record AddReviewCommand : IRequest<Guid>
    {
        public Guid ProductId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
