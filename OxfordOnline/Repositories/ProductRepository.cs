using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OxfordOnline.Data;
using OxfordOnline.Models;
using OxfordOnline.Models.Dto;
using OxfordOnline.Models.Enums;
using OxfordOnline.Repositories.Interfaces;
using OxfordOnline.Services;
using System.IO.Compression;

namespace OxfordOnline.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly IImageRepository _imageRepository;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(AppDbContext context, IImageRepository imageRepository, ILogger<ProductRepository> logger)
        {
            _context = context;
            _imageRepository = imageRepository;
            _logger = logger;
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

        public async Task<IEnumerable<ProductApp>> GetAppSearchAsync(AppProductFilterRequest filterRequest)
        {
            var query = from p in _context.Product
                        join o in _context.Oxford on p.ProductId equals o.ProductId into oxJoin
                        from ox in oxJoin.DefaultIfEmpty()
                        where p.Status == true
                        select new { p, ox };

            // Aplica os filtros baseados no AppProductFilterRequest
            if (filterRequest.ProductId != null && filterRequest.ProductId.Any())
                query = query.Where(q => filterRequest.ProductId.Contains(q.p.ProductId));

            // Nota: O filtro de Barcode foi removido, pois não está presente no AppProductFilterRequest fornecido.
            // Se precisar dele, adicione-o ao AppProductFilterRequest.

            if (filterRequest.FamilyId != null && filterRequest.FamilyId.Any())
                query = query.Where(q => q.ox != null && filterRequest.FamilyId.Contains(q.ox.FamilyId));

            if (filterRequest.BrandId != null && filterRequest.BrandId.Any())
                query = query.Where(q => q.ox != null && filterRequest.BrandId.Contains(q.ox.BrandId));

            if (filterRequest.LineId != null && filterRequest.LineId.Any())
                query = query.Where(q => q.ox != null && filterRequest.LineId.Contains(q.ox.LineId));

            if (filterRequest.DecorationId != null && filterRequest.DecorationId.Any())
                query = query.Where(q => q.ox != null && filterRequest.DecorationId.Contains(q.ox.DecorationId));

            // Trata o filtro de Nome como um texto único
            if (!string.IsNullOrWhiteSpace(filterRequest.Name))
            {
                query = query.Where(q => q.p.ProductName != null && q.p.ProductName.Contains(filterRequest.Name));
            }

            // Primeiro, executamos a consulta para buscar os produtos do banco de dados
            var productsFromDb = await query
                .OrderBy(q => q.p.ProductId)
                .Take(20)
                .Select(q => q.p)
                .ToListAsync();

            // Em seguida, criamos a lista que será retornada
            var productAppList = new List<ProductApp>();

            // Iteramos sobre cada produto para buscar e processar as imagens
            foreach (var p in productsFromDb)
            {
                var productId = p.ProductId;

                // Cria o objeto DTO
                var productApp = new ProductApp
                {
                    ProductId = p.ProductId,
                    Barcode = p.Barcode,
                    Name = p.ProductName
                };

                try
                {
                    // Recupera as imagens do produto
                    var images = await _imageRepository.GetByProductIdAsync(productId, Finalidade.PRODUTO, true);

                    // Verifica se há imagens e, se sim, cria o zip em Base64
                    if (images != null && images.Any())
                    {
                        using (var zipStream = new MemoryStream())
                        {
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
                                        var stream = await _imageRepository.DownloadFileStreamFromFtpAsync(ftpRelativePath);
                                        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                                        using (var entryStream = entry.Open())
                                        {
                                            await stream.CopyToAsync(entryStream);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"*** Erro ao adicionar imagem {fileName} no zip para o produto {productId} ***");
                                    }
                                }
                            }

                            // Converte o MemoryStream do zip para array de bytes e depois para Base64
                            var zipBytes = zipStream.ToArray();
                            productApp.ImageZipBase64 = Convert.ToBase64String(zipBytes);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"*** Erro ao processar imagens para o produto {productId} ***");
                    // Se ocorrer um erro, ImageZipBase64 ficará nulo ou string vazia, o que é aceitável.
                }

                productAppList.Add(productApp);
            }

            return productAppList;
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
                        join o in _context.Oxford on p.ProductId equals o.ProductId
                        where p.Status
                        select new { Product = p, Oxford = o };

            // Aplica filtros dinamicamente
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

            // Executa a primeira parte do query
            var partial = await query.ToListAsync();

            // Carrega relacionamentos externos separadamente (reduz problemas de join duplicado)
            var productIds = partial.Select(x => x.Product.ProductId).ToList();

            var invents = await _context.Invent
                .Where(i => productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId);

            var inventDims = await _context.InventDim
                .Where(i => productIds.Contains(i.ProductId))
                .GroupBy(i => i.ProductId)
                .ToDictionaryAsync(g => g.Key, g => g.First());

            var images = await _context.Image
                .Where(i => productIds.Contains(i.ProductId) && i.ImageMain && i.Finalidade == "PRODUTO")
                .GroupBy(i => i.ProductId)
                .ToDictionaryAsync(g => g.Key, g => g.First().ImagePath);

            // Projeta final
            var list = partial
                .Select(q =>
                {
                    var inv = invents.GetValueOrDefault(q.Product.ProductId);
                    var dim = inventDims.GetValueOrDefault(q.Product.ProductId);
                    var img = images.GetValueOrDefault(q.Product.ProductId);

                    return new ProductOxford
                    {
                        ProductId = q.Product.ProductId,
                        Name = q.Product.ProductName ?? string.Empty,
                        Price = dim?.Price ?? 0,
                        Quantity = dim?.Quantity ?? 0,
                        Brand = q.Oxford.BrandDescription ?? string.Empty,
                        Line = q.Oxford.LineDescription ?? string.Empty,
                        Decoration = q.Oxford.DecorationDescription ?? string.Empty,
                        ThumbUrl = img ?? string.Empty,
                        Invent = new ProductInvent
                        {
                            NetWeight = inv?.NetWeight ?? 0,
                            TaraWeight = inv?.TaraWeight ?? 0,
                            GrossWeight = inv?.GrossWeight ?? 0,
                            GrossDepth = inv?.GrossDepth ?? 0,
                            GrossWidth = inv?.GrossWidth ?? 0,
                            GrossHeight = inv?.GrossHeight ?? 0,
                            UnitVolume = inv?.UnitVolume ?? 0,
                            UnitVolumeML = inv?.UnitVolumeML ?? 0,
                            NrOfItems = inv?.NrOfItems ?? 0,
                            UnitId = inv?.UnitId ?? string.Empty
                        }
                    };
                }).ToList();

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
