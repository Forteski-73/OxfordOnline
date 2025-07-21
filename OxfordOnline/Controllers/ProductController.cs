using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxfordOnline.Models;
using OxfordOnline.Services;
using OxfordOnline.Resources;

namespace OxfordOnline.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: /product
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        // GET: /product/{productId}
        [Authorize]
        [HttpGet("{productId}")]
        public async Task<ActionResult<Product>> GetProductById(string productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = EndPointsMessages.ProductNotFound });

            return Ok(product);
        }

        // POST: /product
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateProducts([FromBody] List<Product> products)
        {
            if (products == null || products.Count == 0)
                return BadRequest(new { message = EndPointsMessages.InvalidProductList });

            try
            {
                await _productService.CreateOrUpdateProductsAsync(products);
                return Ok(new { message = string.Format(EndPointsMessages.ProductSavedSuccess, products.Count) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, EndPointsMessages.LogErrorSavingProduct);
                return StatusCode(500, new
                {
                    message = EndPointsMessages.ErrorSavingProducts,
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // PUT: /product/{productId}
        [Authorize]
        [HttpPut("{productId}")]
        public async Task<IActionResult> UpdateProduct(string productId, [FromBody] Product product)
        {
            if (product == null || productId != product.ProductId)
                return BadRequest(new { message = EndPointsMessages.InvalidProductData });

            try
            {
                var existing = await _productService.GetProductByIdAsync(productId);
                if (existing == null)
                    return NotFound(new { message = EndPointsMessages.ProductNotFoundForUpdate });

                await _productService.UpdateProductAsync(product);
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, EndPointsMessages.LogErrorUpdatingProduct, productId);
                return StatusCode(500, new
                {
                    message = EndPointsMessages.ErrorUpdatingProduct,
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // DELETE: /product/{productId}
        [Authorize]
        [HttpDelete("{productId}")]
        public async Task<IActionResult> DeleteProduct(string productId)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(productId);
                if (!success)
                    return NotFound(new { message = EndPointsMessages.ProductNotFoundForDelete });

                return Ok(new { message = EndPointsMessages.ProductDeletedSuccess });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, EndPointsMessages.LogErrorDeletingProduct, productId);
                return StatusCode(500, new
                {
                    message = EndPointsMessages.ErrorDeletingProduct,
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // GET: /product/details/{productId}
        [Authorize]
        [HttpGet("productData/{productId}")]
        public async Task<ActionResult<ProductData>> GetProductDetails(string productId)
        {
            var details = await _productService.GetProductDataAsync(productId);
            if (details == null)
                return NotFound(new { message = EndPointsMessages.ProductNotFound });

            return Ok(details);
        }

        // GET: /product/productDataRange/{id}
        [Authorize]
        [HttpGet("productDataRange/{offsetId}")]
        public async Task<ActionResult<List<ProductData>>> GetProductDataRange(int offsetId)
        {
            var resultList = new List<ProductData>();

            if(offsetId == 0)
            {
                var product = await _productService.GetFirstAsync();
                if (product != null && int.TryParse(product.ProductId, out int parsedId))
                {
                    offsetId = parsedId;
                }
            }

            for (int i = offsetId; i < offsetId + 1000; i++)
            {
                var id = i.ToString("D6"); // Formata o ID para 6 dígitos
                var details = await _productService.GetProductDataRangeAsync(id);
                if (details != null)
                {
                    resultList.Add(details);
                }
            }

            if (!resultList.Any())
                return NotFound(new { message = "Nenhum produto encontrado no intervalo solicitado." });

            return Ok(resultList);
        }

        // POST: /product/productDataByIds
        [Authorize]
        [HttpPost("productsData")]
        public async Task<ActionResult<List<ProductData>>> GetProductDataByIds([FromBody] List<string> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest(new { message = "A lista de productIds está vazia ou nula." });

            var result = new List<ProductData>();

            foreach (var productId in productIds)
            {
                var data = await _productService.GetProductDataAsync(productId);
                if (data != null)
                {
                    result.Add(data);
                }
            }

            if (!result.Any())
                return NotFound(new { message = "Nenhum dado encontrado para os productIds informados." });

            return Ok(result);
        }
    }
}