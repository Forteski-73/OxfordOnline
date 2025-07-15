using OxfordOnline.Models;
using OxfordOnline.Repositories;
using OxfordOnline.Repositories.Interfaces;

namespace OxfordOnline.Services
{
    public class ImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IProductRepository _productRepository;

        public ImageService(IImageRepository imageRepository, IProductRepository productRepository)
        {
            _imageRepository = imageRepository;
            _productRepository = productRepository;
        }

        public async Task<Image?> GetImageByIdAsync(int id)
        {
            return await _imageRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Image>> GetImagesByProductIdAsync(string productId)
        {
            return await _imageRepository.GetByProductIdAsync(productId);
        }

        public async Task<IEnumerable<Image>> CreateOrReplaceImagesAsync(List<Image> images)
        {
            var productIds = images.Select(i => i.ProductId).Distinct().ToList();

            // Consultar o repositório correto para produtos
            //var existingProducts = await _imageRepository.GetByProductIdsAsync(productIds);
            var existingProducts = await _productRepository.GetByProductListIdsAsync(productIds);
            var existingProductIds = existingProducts.Select(p => p.ProductId).ToList();

            var missingProductIds = productIds.Except(existingProductIds).ToList();

            if (missingProductIds.Any())
                throw new ArgumentException($"Produtos não encontrados: {string.Join(", ", missingProductIds)}");

            // Remover imagens antigas dos produtos recebidos
            await _imageRepository.RemoveByProductIdsAsync(productIds);

            // Evitar rastreamento duplicado no EF
            foreach (var img in images)
                img.Product = null;

            await _imageRepository.AddRangeAsync(images);
            await _imageRepository.SaveAsync();

            return images;
        }
    }
}
