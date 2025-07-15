using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OxfordOnline.Models;
using OxfordOnline.Resources;
using OxfordOnline.Services;

namespace OxfordOnline.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ImageService _imageService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ImageService imageService, ILogger<ImageController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        // POST: /Image
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateImages([FromBody] List<Image> images)
        {
            if (images == null || !images.Any())
                return BadRequest(new { message = EndPointsMessages.NoImagesProvided });

            if (images.Any(i => string.IsNullOrWhiteSpace(i.ProductId) || string.IsNullOrWhiteSpace(i.ImagePath)))
                return BadRequest(new { message = EndPointsMessages.InvalidImageField });

            try
            {
                var result = await _imageService.CreateOrReplaceImagesAsync(images);
                return Ok(new
                {
                    message = string.Format(EndPointsMessages.ImageSavedSuccess, result.Count()),
                    imagens = result
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, EndPointsMessages.LogErrorSavingImage);
                return StatusCode(500, new
                {
                    message = EndPointsMessages.ErrorSavingImages,
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // GET: /Image/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Image>> GetImageById(int id)
        {
            var image = await _imageService.GetImageByIdAsync(id);
            if (image == null)
                return NotFound(new { message = EndPointsMessages.ImageNotFound });

            return Ok(image);
        }

        // GET: /Image/Product/{productId}
        [Authorize]
        [HttpGet("Product/{productId}")]
        public async Task<ActionResult<IEnumerable<Image>>> GetImagesByProductId(string productId)
        {
            var images = await _imageService.GetImagesByProductIdAsync(productId);
            if (!images.Any())
                return NotFound(new { message = EndPointsMessages.ImageNotFoundForProduct });

            return Ok(images);
        }
    }
}