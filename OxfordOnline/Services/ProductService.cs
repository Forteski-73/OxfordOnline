using Microsoft.EntityFrameworkCore;
using OxfordOnline.Models;
using OxfordOnline.Models.Enums;
using OxfordOnline.Repositories.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace OxfordOnline.Services
{
    public class ProductService
    {
        private readonly IProductRepository         _repo;
        private readonly IOxfordRepository          _oxfordRepo;
        private readonly IInventRepository          _inventRepo;
        private readonly ITaxInformationRepository  _taxRepo;
        //private readonly IImageRepository           _imageRepo;

        public ProductService(
            IProductRepository          repo,
            IOxfordRepository           oxfordRepo,
            IInventRepository           inventRepo,
            ITaxInformationRepository   taxRepo)
            //IImageRepository            imageRepo
        {
            _repo       = repo;
            _oxfordRepo = oxfordRepo;
            _inventRepo = inventRepo;
            _taxRepo    = taxRepo;
            //_imageRepo  = imageRepo;
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

        public async Task<ProductData?> GetProductDataAsync(string productId)
        {
            var product = await _repo.GetByProductIdAsync(productId);
            if (product == null) return null;

            var oxford = await _oxfordRepo.GetByProductIdAsync(productId);
            var invent = await _inventRepo.GetByProductIdAsync(productId);
            var taxInfo = await _taxRepo.GetByProductIdAsync(productId);
            //var images = await _imageRepo.GetByProductIdAsync(productId, Finalidade.TODOS);

            return new ProductData
            {
                Product = product,
                Oxford = oxford,
                Invent = invent,
                TaxInformation = taxInfo,
            };
        }
        public async Task<ProductData?> GetProductDataRangeAsync(string Id)
        {
            var product = await _repo.GetByProductIdAsync(Id);
            if (product == null) return null;

            var oxford      = await _oxfordRepo.GetByProductIdAsync(product.ProductId);
            var invent      = await _inventRepo.GetByProductIdAsync(product.ProductId);
            var taxInfo     = await _taxRepo.GetByProductIdAsync(product.ProductId);
            //var images    = await _imageRepo.GetByProductIdAsync(productId, Finalidade.TODOS);

            return new ProductData
            {
                Product         = product,
                Oxford          = oxford,
                Invent          = invent,
                TaxInformation  = taxInfo,
            };
        }

        public async Task<Product?> GetFirstAsync()
        {
            var prod = await _repo.GetFirstAsync();
            return prod;
        }
    }
}
