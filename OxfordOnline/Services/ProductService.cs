using OxfordOnline.Models;
using OxfordOnline.Repositories.Interfaces;

namespace OxfordOnline.Services
{
    public class ProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
            await _repo.GetAllAsync();

        public async Task<Product?> GetProductByIdAsync(String productId) =>
            await _repo.GetByProductIdAsync(productId);

        public async Task<bool> CreateOrUpdateProductsAsync(List<Product> products)
        {
            foreach (var p in products)
            {
                var existProduct = await _repo.GetByProductIdAsync(p.ProductId);
                if (existProduct != null)
                {
                    existProduct.ProductName = p.ProductName;
                    existProduct.Barcode = p.Barcode;
                    await _repo.UpdateAsync(existProduct);
                }
                else
                {
                    await _repo.AddAsync(p);
                }
            }

            await _repo.SaveAsync();
            return true;
        }
        public async Task<bool> UpdateProductAsync(Product product)
        {
            await _repo.UpdateAsync(product);
            await _repo.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(String productId)
        {
            var product = await _repo.GetByProductIdAsync(productId);
            if (product == null) return false;

            await _repo.DeleteAsync(product);
            await _repo.SaveAsync();
            return true;
        }
    }
}
