namespace CasterSimulator.Models
{
    public class Product
    {
        public long SequenceId { get; set; }
        public int CutNumber { get; set; }
        public string ProductId { get; set; }
        public double LengthAimMeters { get; set; }
        
        public double LengthMax { get; set; }
        
        public double LengthMin { get; set; }

        public double CutLength { get; set; }
        
        public double Width { get; set; }
        
        public double Thickness { get; set; }

        public double Weight { get; set; }

        public double CastLengthStart { get; set; }

        public Product(long sequenceId,int cutNumber, string productId, double lengthAim, double lengthMin, double lengthMax)
        {
            SequenceId = sequenceId;
            CutNumber = cutNumber;
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            LengthAimMeters = lengthAim > 0 ? lengthAim : throw new ArgumentException("Product length must be positive.", nameof(lengthAim));
            LengthMax = lengthMax > 0 ? lengthMax : throw new ArgumentException("Product length must be positive.", nameof(lengthMax));
            LengthMin  = lengthMin > 0 ? lengthMin : throw new ArgumentException("Product length must be positive.", nameof(lengthMin));
        }

        public Product() { }

        public Product Clone()
        {
            return new Product
            {
                CutNumber = this.CutNumber,
                ProductId = this.ProductId,
                LengthAimMeters = this.LengthAimMeters,
                LengthMax = this.LengthMax,
                LengthMin = this.LengthMin,
                CutLength = this.CutLength
            };
        }
    }
}