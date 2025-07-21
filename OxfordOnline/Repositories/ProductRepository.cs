using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Repositories.Interfaces;

namespace OxfordOnline.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Product.ToListAsync();

        public async Task<Product?> GetByProductIdAsync(string productId) =>
            await _context.Product.FirstOrDefaultAsync(p => p.ProductId == productId);

        public async Task<List<Product>> GetByProductListIdsAsync(List<string> products) =>
            await _context.Product
                .Where(p => products.Contains(p.ProductId))
                .ToListAsync();

        public async Task<Product?> GetFirstAsync() =>
            await _context.Product.FirstOrDefaultAsync();

        public async Task AddAsync(Product product) =>
            await _context.Product.AddAsync(product);

        public async Task UpdateAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Product product) =>
            _context.Product.Remove(product);

        public async Task SaveAsync() =>
            await _context.SaveChangesAsync();

    }
}
