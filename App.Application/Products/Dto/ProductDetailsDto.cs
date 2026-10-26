using App.Application.Reviews.Dto;

namespace App.Application.Products.Dto
{
    public class ProductDetailsDto
    {
        public ProductResponse Product { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = [];
        public int AverageRating => Reviews.Count == 0 ? 0 : (int)Math.Floor(Reviews.Average(r => r.Rating));
        public int ReviewCount => Reviews.Count;
    }
}
