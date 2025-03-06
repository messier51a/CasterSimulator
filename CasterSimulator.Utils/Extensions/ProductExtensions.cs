using System.Globalization;
using System.Text;
using System.Text.Json;
using CasterSimulator.Models;

namespace CasterSimulator.Utils.Extensions
{
    public static class ProductExtensions
    {
        public static string ToInfluxLineProtocol(this IEnumerable<Product> products, string area)
        {
            if (products == null || !products.Any())
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var product in products)
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    $"{area}," +
                    $"product_id=\"{product.ProductId}\" " +
                   // $"cut_number={product.CutNumber}," +
                    $"length_aim={product.LengthAimMeters}," +
                    $"length_min={product.LengthMin}," +
                    $"length_max={product.LengthMax}," +
                    $"width={product.Width}," +
                    $"thickness={product.Thickness}," +
                    $"weight={product.Weight}\n");
            }

            return sb.ToString();
        }
    }
}