using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OxfordOnline.Controllers;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Models.Enums;
using OxfordOnline.Repositories.Interfaces;
using OxfordOnline.Services;
using System.Net;

namespace OxfordOnline.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly AppDbContext _context;
        private readonly IFtpService _ftpService;
        private readonly ILogger<ImageController> _logger;

        public ImageRepository(AppDbContext context, IFtpService ftpService, ILogger<ImageController> logger)
        {
            _context = context;
            _ftpService = ftpService;
            _logger = logger;
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            return await _context.Image.FindAsync(id);
        }

        public async Task<IEnumerable<Image>> GetByProductIdAsync(string productId, Finalidade finalidade)
        {
            var query = _context.Image
                .Where(i => i.ProductId == productId);

            if (finalidade != Finalidade.TODOS)
            {
                query = query.Where(i => i.Finalidade == finalidade.ToString());
            }

            return await query
                .OrderBy(i => i.Sequence)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Image> images)
        {
            await _context.Image.AddRangeAsync(images);
        }

        public async Task RemoveByProductIdsAsync(IEnumerable<string> productIds)
        {
            var imagesToRemove = await _context.Image
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();

            if (imagesToRemove.Any())
                _context.Image.RemoveRange(imagesToRemove);
        }

        public async Task<IEnumerable<Image>> GetByProductIdsAsync(List<string> productIds)
        {
            return await _context.Image
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateImagesByProductIdAsync(string productId, List<IFormFile> files)
        {
            _logger.LogError("**** INICIO  UpdateImagesByProductIdAsync ****");
            try
            {
                var images = await _context.Image.Where(i => i.ProductId == productId).ToListAsync();

                var firstImage = images.FirstOrDefault();
                if (firstImage != null)
                {
                    _logger.LogError("**** INICIO  FTP ****");
                    foreach (var image in images)
                    {
                        if (!string.IsNullOrEmpty(image.ImagePath))
                        {
                            await _ftpService.DeleteAsync(image.ImagePath); // remove a imagem do FTP
                        }
                        _context.Image.Remove(image);  // remove a imagem do contexto
                    }

                    foreach (var file in files)
                    {
                        _logger.LogError("**** INICIO  IMAGENS ****");
                        if (file.Length == 0) continue;

                        using var stream = file.OpenReadStream();

                        var baseDir = Path.GetDirectoryName(firstImage.ImagePath)?.Replace("\\", "/");
                        var ftpPath = $"{baseDir}/{file.FileName}";

                        await _ftpService.UploadAsync(ftpPath, stream);

                        var imageNew = new Image
                        {
                            ProductId = productId,
                            ImagePath = ftpPath,
                            //UploadedAt = DateTime.UtcNow
                        };
                        _context.Image.Add(imageNew); // adiciona a nova imagem ao contexto
                    }
                    _logger.LogError("**** ATUALIZOU DB ****");
                    await _context.SaveChangesAsync(); // atualiza o banco de dados

                }
                else
                {
                    _logger.LogError("**** Nenhuma imagem existente para basear o caminho. ****");
                    throw new InvalidOperationException("Nenhuma imagem existente para basear o caminho.");
                }
            }
            catch (Exception ex)
            {
                // Registra o erro com mais contexto
                _logger.LogError(ex, $"**** Erro ao atualizar imagens para o produto: {productId} ****");

                // Repropaga a exceção para ser tratada no controller (ou middleware)
                throw;
            }
        }

        /*
        public async Task DeleteImagesByProductIdAsync(string productId)
        {
            var images = await _context.Image.Where(i => i.ProductId == productId).ToListAsync();

            foreach (var image in images)
            {
                if (!string.IsNullOrEmpty(image.ImagePath))
                {
                    await _ftpService.DeleteAsync(image.ImagePath);
                }
                //_context.Image.Remove(image);  voltar para remover do banco de dados
            }

            //await _context.SaveChangesAsync(); voltar para remover do banco de dados
        }
        */

        /*
        public async Task SaveImageAsync(Image image, string productId, string pathFtp, string fileName, Stream content)
        {
            // Gera o caminho completo combinando o diretório recebido + guid + filename
            var ftpPath = $"{pathFtp.TrimEnd('/')}/{fileName}";
            await _ftpService.UploadAsync(ftpPath, content);

            var image = new Image
            {
                ProductId = productId,
                ImagePath = ftpPath,
                //UploadedAt = DateTime.UtcNow
            };

            _context.Image.Add(image);
            await _context.SaveChangesAsync();
        }
        */

        public async Task<Stream> DownloadFileStreamFromFtpAsync(string ftpFilePath)
        {
            return await _ftpService.DownloadAsync(ftpFilePath);
        }

        /* public async Task<Stream> DownloadFileStreamFromFtpAsync(string ftpFilePath)
         {
             var ftpHost = "ftp.oxfordtec.com.br";
             var ftpUser = "u700242432.oxfordftp";
             var ftpPass = "OxforEstrutur@25";

             var ftpUri = new Uri($"ftp://{ftpHost}/{ftpFilePath}");
             var request = (FtpWebRequest)WebRequest.Create(ftpUri);
             request.Method = WebRequestMethods.Ftp.DownloadFile;
             request.Credentials = new NetworkCredential(ftpUser, ftpPass);
             request.UseBinary = true;
             request.UsePassive = true;
             request.KeepAlive = false;

             var response = (FtpWebResponse)await request.GetResponseAsync();
             // Retorna o stream da resposta para leitura externa
             return response.GetResponseStream();
         }*/
    }
}
