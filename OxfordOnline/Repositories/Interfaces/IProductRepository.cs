using OxfordOnline.Models;

namespace OxfordOnline.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByProductIdAsync(string productId);
        Task<List<Product>> GetByProductListIdsAsync(List<string> productIds);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetFirstAsync();
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task SaveAsync();
    }
}
