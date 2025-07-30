using Microsoft.EntityFrameworkCore;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Models.Dto;
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

        public async Task<IEnumerable<Product>> GetSearchAsync(
            string? product,
            string? barcode,
            string? family,
            string? brand,
            string? line,
            string? decoration,
            string? nome)
        {
            var query = from p in _context.Product
                        join o in _context.Oxford on p.ProductId equals o.ProductId into oxJoin
                        from ox in oxJoin.DefaultIfEmpty()
                        where p.Status == true
                        select new { p, ox };

            if (!string.IsNullOrWhiteSpace(product))
                query = query.Where(q => q.p.ProductId.Contains(product));

            if (!string.IsNullOrWhiteSpace(barcode))
                query = query.Where(q => q.p.Barcode != null && q.p.Barcode.Contains(barcode));

            if (!string.IsNullOrWhiteSpace(family))
                query = query.Where(q => q.ox != null && q.ox.FamilyDescription != null && q.ox.FamilyDescription.Contains(family));

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(q => q.ox != null && q.ox.BrandDescription != null && q.ox.BrandDescription.Contains(brand));

            if (!string.IsNullOrWhiteSpace(line))
                query = query.Where(q => q.ox != null && q.ox.LineDescription != null && q.ox.LineDescription.Contains(line));

            if (!string.IsNullOrWhiteSpace(decoration))
                query = query.Where(q => q.ox != null && q.ox.DecorationDescription != null && q.ox.DecorationDescription.Contains(decoration));

            if (!string.IsNullOrWhiteSpace(nome))
                query = query.Where(q => q.p.ProductName != null && q.p.ProductName.Contains(nome));

            return await query
                .OrderBy(q => q.p.ProductId)
                .Take(21)
                .Select(q => q.p)
                .ToListAsync();
        }

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

        public async Task<List<Product>> GetByStatusAndLocationAsync(bool status, string locationId)
        {
            return await _context.Product
                .Where(p => p.Status == status)
                .Join(
                    _context.InventDim,
                    p => p.ProductId,
                    i => i.ProductId,
                    (p, i) => new { Product = p, InventDim = i }
                )
                .Where(x => x.InventDim.LocationId == locationId)
                .Select(x => x.Product)
                .Distinct()
                .ToListAsync();
        }

        /*
        public async Task<List<ProductOxford>> GetProductOxfordAsync(ProductOxfordFilters filters)
        {
            var query = from p in _context.Product
                        where p.Status
                        join o in _context.Oxford on p.ProductId equals o.ProductId
                        join i in _context.Invent on p.ProductId equals i.ProductId into inventGroup
                        from invent in inventGroup.DefaultIfEmpty()
                        join id in _context.InventDim on p.ProductId equals id.ProductId into inventDimGroup
                        from inventDim in inventDimGroup.DefaultIfEmpty()
                        select new
                        {
                            Product = p,
                            Oxford = o,
                            Invent = invent,
                            InventDim = inventDim
                        };

            // Aplica filtros dinamicamente antes da projeção
            if (filters.FamilyId?.Any() == true)
                query = query.Where(q => filters.FamilyId.Contains(q.Oxford.FamilyId));
            if (filters.BrandId?.Any() == true)
                query = query.Where(q => filters.BrandId.Contains(q.Oxford.BrandId));
            if (filters.DecorationId?.Any() == true)
                query = query.Where(q => filters.DecorationId.Contains(q.Oxford.DecorationId));
            if (filters.TypeId?.Any() == true)
                query = query.Where(q => filters.TypeId.Contains(q.Oxford.TypeId));
            if (filters.ProcessId?.Any() == true)
                query = query.Where(q => filters.ProcessId.Contains(q.Oxford.ProcessId));
            if (filters.SituationId?.Any() == true)
                query = query.Where(q => filters.SituationId.Contains(q.Oxford.SituationId));
            if (filters.LineId?.Any() == true)
                query = query.Where(q => filters.LineId.Contains(q.Oxford.LineId));
            if (filters.QualityId?.Any() == true)
                query = query.Where(q => filters.QualityId.Contains(q.Oxford.QualityId));
            if (filters.BaseProductId?.Any() == true)
                query = query.Where(q => filters.BaseProductId.Contains(q.Oxford.BaseProductId));
            if (filters.ProductGroupId?.Any() == true)
                query = query.Where(q => filters.ProductGroupId.Contains(q.Oxford.ProductGroupId));


            var sql = query.ToQueryString();
            Console.WriteLine(sql); // Ou logue com ILogger

            // Projeta para ProductOxford diretamente no banco (evita trazer dados desnecessários)
            var list = await query.Select(q => new ProductOxford
            {
                ProductId = q.Product.ProductId,
                Name = q.Product.ProductName ?? string.Empty,
                Price = q.InventDim != null ? q.InventDim.Price.GetValueOrDefault() : 0,
                Quantity = q.InventDim != null ? q.InventDim.Quantity.GetValueOrDefault() : 0,
                Brand = q.Oxford.BrandDescription ?? string.Empty,
                Line = q.Oxford.LineDescription ?? string.Empty,
                Decoration = q.Oxford.DecorationDescription ?? string.Empty,
                ThumbUrl = string.Empty,

                Invent = new ProductInvent
                {
                    NetWeight = q.Invent != null ? q.Invent.NetWeight.GetValueOrDefault() : 0,
                    TaraWeight = q.Invent != null ? q.Invent.TaraWeight.GetValueOrDefault() : 0,
                    GrossWeight = q.Invent != null ? q.Invent.GrossWeight.GetValueOrDefault() : 0,
                    GrossDepth = q.Invent != null ? q.Invent.GrossDepth.GetValueOrDefault() : 0,
                    GrossWidth = q.Invent != null ? q.Invent.GrossWidth.GetValueOrDefault() : 0,
                    GrossHeight = q.Invent != null ? q.Invent.GrossHeight.GetValueOrDefault() : 0,
                    UnitVolume = q.Invent != null ? q.Invent.UnitVolume.GetValueOrDefault() : 0,
                    UnitVolumeML = q.Invent != null ? q.Invent.UnitVolumeML.GetValueOrDefault() : 0,
                    NrOfItems = q.Invent != null ? q.Invent.NrOfItems.GetValueOrDefault() : 0,
                    UnitId = q.Invent != null ? q.Invent.UnitId : string.Empty
                }
            }).ToListAsync();

            return list;
        }
        */

        public async Task<List<ProductOxford>> GetProductOxfordAsync(ProductOxfordFilters filters)
        {
            var query = from p in _context.Product
                        where p.Status
                        join o in _context.Oxford on p.ProductId equals o.ProductId
                        join i in _context.Invent on p.ProductId equals i.ProductId into inventGroup
                        from invent in inventGroup.DefaultIfEmpty()
                        join id in _context.InventDim on p.ProductId equals id.ProductId into inventDimGroup
                        from inventDim in inventDimGroup.DefaultIfEmpty()
                        join img in _context.Image
                            .Where(x => x.ImageMain && x.Finalidade == "PRODUTO")
                            on p.ProductId equals img.ProductId into imageGroup
                        from image in imageGroup.DefaultIfEmpty()
                        select new
                        {
                            Product = p,
                            Oxford = o,
                            Invent = invent,
                            InventDim = inventDim,
                            ImagePath = image.ImagePath
                        };

            // Aplica filtros dinamicamente antes da projeção
            if (filters.FamilyId?.Any() == true)
                query = query.Where(q => filters.FamilyId.Contains(q.Oxford.FamilyId));
            if (filters.BrandId?.Any() == true)
                query = query.Where(q => filters.BrandId.Contains(q.Oxford.BrandId));
            if (filters.DecorationId?.Any() == true)
                query = query.Where(q => filters.DecorationId.Contains(q.Oxford.DecorationId));
            if (filters.TypeId?.Any() == true)
                query = query.Where(q => filters.TypeId.Contains(q.Oxford.TypeId));
            if (filters.ProcessId?.Any() == true)
                query = query.Where(q => filters.ProcessId.Contains(q.Oxford.ProcessId));
            if (filters.SituationId?.Any() == true)
                query = query.Where(q => filters.SituationId.Contains(q.Oxford.SituationId));
            if (filters.LineId?.Any() == true)
                query = query.Where(q => filters.LineId.Contains(q.Oxford.LineId));
            if (filters.QualityId?.Any() == true)
                query = query.Where(q => filters.QualityId.Contains(q.Oxford.QualityId));
            if (filters.BaseProductId?.Any() == true)
                query = query.Where(q => filters.BaseProductId.Contains(q.Oxford.BaseProductId));
            if (filters.ProductGroupId?.Any() == true)
                query = query.Where(q => filters.ProductGroupId.Contains(q.Oxford.ProductGroupId));

            var sql = query.ToQueryString();
            Console.WriteLine(sql); // Ou logue com ILogger

            // Projeta para ProductOxford diretamente no banco
            var list = await query.Select(q => new ProductOxford
            {
                ProductId = q.Product.ProductId,
                Name = q.Product.ProductName ?? string.Empty,
                Price = q.InventDim != null ? q.InventDim.Price.GetValueOrDefault() : 0,
                Quantity = q.InventDim != null ? q.InventDim.Quantity.GetValueOrDefault() : 0,
                Brand = q.Oxford.BrandDescription ?? string.Empty,
                Line = q.Oxford.LineDescription ?? string.Empty,
                Decoration = q.Oxford.DecorationDescription ?? string.Empty,
                ThumbUrl = q.ImagePath ?? string.Empty,

                Invent = new ProductInvent
                {
                    NetWeight = q.Invent != null ? q.Invent.NetWeight.GetValueOrDefault() : 0,
                    TaraWeight = q.Invent != null ? q.Invent.TaraWeight.GetValueOrDefault() : 0,
                    GrossWeight = q.Invent != null ? q.Invent.GrossWeight.GetValueOrDefault() : 0,
                    GrossDepth = q.Invent != null ? q.Invent.GrossDepth.GetValueOrDefault() : 0,
                    GrossWidth = q.Invent != null ? q.Invent.GrossWidth.GetValueOrDefault() : 0,
                    GrossHeight = q.Invent != null ? q.Invent.GrossHeight.GetValueOrDefault() : 0,
                    UnitVolume = q.Invent != null ? q.Invent.UnitVolume.GetValueOrDefault() : 0,
                    UnitVolumeML = q.Invent != null ? q.Invent.UnitVolumeML.GetValueOrDefault() : 0,
                    NrOfItems = q.Invent != null ? q.Invent.NrOfItems.GetValueOrDefault() : 0,
                    UnitId = q.Invent != null ? q.Invent.UnitId : string.Empty
                }
            }).ToListAsync();

            return list;
        }
        public async Task<List<ProductOxfordDetails>> GetFilteredOxfordProductDetailsAsync(List<string> products)
        {
            // Consulta agrupada por produto
            var query = from p in _context.Product
                        where p.Status && products.Contains(p.ProductId)
                        join o in _context.Oxford on p.ProductId equals o.ProductId
                        join i in _context.Invent on p.ProductId equals i.ProductId into inventGroup
                        from invent in inventGroup.DefaultIfEmpty()
                        join id in _context.InventDim on p.ProductId equals id.ProductId into inventDimGroup
                        from inventDim in inventDimGroup.DefaultIfEmpty()
                        select new
                        {
                            Product = p,
                            Oxford = o,
                            Invent = invent,
                            InventDim = inventDim
                        };

            var baseData = await query
                .GroupBy(x => x.Product.ProductId) // evitar repetições
                .Select(g => g.First())            // pegar só um por produto
                .ToListAsync();

            // Carrega imagens separadamente
            var productIds = baseData.Select(x => x.Product.ProductId).ToList();

            var imagesByProduct = await _context.Image
                .Where(img => productIds.Contains(img.ProductId) && img.Finalidade == "PRODUTO")
                .OrderBy(img => img.Sequence)
                .GroupBy(img => img.ProductId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(img => img.ImagePath).ToList());

            // Mapeia para DTO final
            var result = baseData.Select(q => new ProductOxfordDetails
            {
                ProductId = q.Product.ProductId,
                Name = q.Product.ProductName ?? string.Empty,
                Price = q.InventDim?.Price ?? 0,
                Quantity = q.InventDim?.Quantity ?? 0,
                Brand = q.Oxford.BrandDescription ?? string.Empty,
                Line = q.Oxford.LineDescription ?? string.Empty,
                Decoration = q.Oxford.DecorationDescription ?? string.Empty,
                ProductDescription = q.Oxford.BaseProductDescription ?? string.Empty,

                Images = (imagesByProduct.ContainsKey(q.Product.ProductId)
                            ? imagesByProduct[q.Product.ProductId]
                            : new List<string>())
                        .Select((img, index) => new { Index = index.ToString(), img })
                        .ToDictionary(x => x.Index, x => x.img),

                Invent = new ProductInvent
                {
                    NetWeight = q.Invent?.NetWeight ?? 0,
                    TaraWeight = q.Invent?.TaraWeight ?? 0,
                    GrossWeight = q.Invent?.GrossWeight ?? 0,
                    GrossDepth = q.Invent?.GrossDepth ?? 0,
                    GrossWidth = q.Invent?.GrossWidth ?? 0,
                    GrossHeight = q.Invent?.GrossHeight ?? 0,
                    UnitVolume = q.Invent?.UnitVolume ?? 0,
                    UnitVolumeML = q.Invent?.UnitVolumeML ?? 0,
                    NrOfItems = q.Invent?.NrOfItems ?? 0,
                    UnitId = q.Invent?.UnitId ?? string.Empty
                }
            }).ToList();

            return result;
        }
    }
}
