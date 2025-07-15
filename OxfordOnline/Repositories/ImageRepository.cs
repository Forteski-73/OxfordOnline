using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Repositories.Interfaces;

namespace OxfordOnline.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly AppDbContext _context;

        public ImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            return await _context.Image.FindAsync(id);
        }

        public async Task<IEnumerable<Image>> GetByProductIdAsync(string productId)
        {
            return await _context.Image
                .Where(i => i.ProductId == productId)
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
    }
}
