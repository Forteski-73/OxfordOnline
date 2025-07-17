using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OxfordOnline.Models;
using OxfordOnline.Resources;
using OxfordOnline.Services;
using System.IO;
using System.IO.Compression;
using System.Net;

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

        [AllowAnonymous]
        [HttpGet("ProductImage/{productId}")]
        public async Task<IActionResult> DownloadZipByProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return BadRequest("Produto inválido.");

            try
            {
                var images = await _imageService.GetImagesByProductIdAsync(productId);

                if (images == null || !images.Any())
                    return NotFound("Nenhuma imagem encontrada para o produto.");

                var zipStream = new MemoryStream(); // NÃO usar 'using' aqui!

                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var img in images)
                    {
                        if (string.IsNullOrWhiteSpace(img.ImagePath))
                            continue;

                        var ftpRelativePath = img.ImagePath.TrimStart('/').Replace('\\', '/');
                        var fileName = Path.GetFileName(ftpRelativePath);

                        try
                        {
                            var stream = await _imageService.DownloadImageStreamAsync(ftpRelativePath);
                            var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                            using var entryStream = entry.Open();
                            await stream.CopyToAsync(entryStream);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Erro ao adicionar imagem {fileName} no zip");
                        }
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin); // Reposiciona antes de retornar

                return File(zipStream, "application/zip", $"produto_{productId}_imagens.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar zip de imagens do produto");
                return StatusCode(500, "Erro interno ao gerar imagens do produto.");
            }
        }

        [HttpPost("ReplaceProductImages/{productId}")]
        public async Task<IActionResult> ReplaceImages(string productId, [FromForm] List<IFormFile> files)
        {
            _logger.LogError("*** INICIO 0 ***");
            if (string.IsNullOrWhiteSpace(productId))
                return BadRequest("Produto inválido.");

            if (files == null || !files.Any())
                return BadRequest("Nenhuma imagem enviada.");

            _logger.LogError("*** INICIO 1 ***");
            try
            {
                /*_logger.LogError("*** DELETE IMAGENS ***");
                // 1. Exclui todas as imagens existentes
                await _imageService.DeleteImagesByProductIdAsync(productId);
                _logger.LogError("*** SALVA IMAGENS INICIO ***");
                */

                await _imageService.UpdateImagesByProductIdAsync(productId, files);


                // 2. Envia as novas imagens para o FTP
                /*foreach (var file in files)
                {
                    if (file.Length == 0) continue;
                    
                    using var stream = file.OpenReadStream();
                    await _imageService.SaveImageAsync(productId, file.FileName, stream);
                }
                _logger.LogError("*** SALVA IMAGENS FIM ***");*/

                return Ok("Imagens substituídas com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "*** Erro ao substituir imagens ***");
                return StatusCode(500, "Erro ao salvar novas imagens.");
            }
        }
    }
}