using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Repositories.Interfaces;

namespace OxfordOnline.Repositories
{
    public class OxfordRepository : IOxfordRepository
    {
        private readonly AppDbContext _context;

        public OxfordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Oxford?> GetByProductIdAsync(string productId) =>
            await _context.Oxford.FirstOrDefaultAsync(o => o.ProductId == productId);

        public async Task<List<Oxford>> GetByProductListIdsAsync(List<string> productIds) =>
            await _context.Oxford
                .Where(o => productIds.Contains(o.ProductId))
                .ToListAsync();

        public async Task AddAsync(Oxford oxford) =>
            await _context.Oxford.AddAsync(oxford);

        public async Task UpdateAsync(Oxford oxford)
        {
            _context.Entry(oxford).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Oxford oxford) =>
            _context.Oxford.Remove(oxford);

        public async Task SaveAsync() =>
            await _context.SaveChangesAsync();
    }
}
