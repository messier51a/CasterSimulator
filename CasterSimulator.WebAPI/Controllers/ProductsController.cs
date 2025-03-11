using CasterSimulator.Models;
using Microsoft.AspNetCore.Mvc;

namespace CasterSimulator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private static List<Product> _products = new();
        
        [HttpGet]
        public IActionResult GetProducts()
        {
            return Ok(_products);
        }
        
        [HttpPost]
        public IActionResult UpdateProducts([FromBody] List<Product> products)
        {
            _products = products;
            return Ok(new { Message = "Products updated successfully." });
        }
    }
}