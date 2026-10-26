namespace App.Application.Reviews.Dto
{
    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public Guid ProductId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
    }
}
