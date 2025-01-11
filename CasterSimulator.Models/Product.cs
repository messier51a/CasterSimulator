namespace CasterSimulator.Models
{
    public class Product
    {
        public string ProductId { get; set; }
        public double ProductLength { get; set; }

        public Product(string productId, double productLength)
        {
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            ProductLength = productLength > 0 ? productLength : throw new ArgumentException("Product length must be positive.", nameof(productLength));
        }
    }
}