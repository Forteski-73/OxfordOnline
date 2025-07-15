using OxfordOnline.Models;

namespace OxfordOnline.Repositories.Interfaces
{
    public interface IImageRepository
    {
        Task<Image?> GetByIdAsync(int id);
        Task<IEnumerable<Image>> GetByProductIdAsync(string productId);
        Task<IEnumerable<Image>> GetByProductIdsAsync(List<string> productIds);
        Task AddRangeAsync(IEnumerable<Image> images);
        Task RemoveByProductIdsAsync(IEnumerable<string> productIds); // atualizado
        Task SaveAsync();
    }
}
