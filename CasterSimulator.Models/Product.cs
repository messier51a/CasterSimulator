namespace CasterSimulator.Models
{
    public class Product
    {
        public string ProductId { get; set; }
        public double LengthAim { get; set; }
        
        public double LengthMax { get; set; }
        
        public double LengthMin { get; set; }

        public Product(string productId, double lengthAim, double lengthMin, double lengthMax)
        {
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            LengthAim = lengthAim > 0 ? lengthAim : throw new ArgumentException("Product length must be positive.", nameof(lengthAim));
            LengthMax = lengthMax > 0 ? lengthMax : throw new ArgumentException("Product length must be positive.", nameof(lengthMax));
            LengthMin  = lengthMin > 0 ? lengthMin : throw new ArgumentException("Product length must be positive.", nameof(lengthMin));
        }
    }
}