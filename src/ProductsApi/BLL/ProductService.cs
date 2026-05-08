using Microsoft.Extensions.Logging;
using ProductsApi.BLL.Interfaces;
using ProductsApi.DAL.Interfaces;
using ProductsApi.DTOs;
using ProductsApi.Models;

namespace ProductsApi.BLL;

/// <summary>
/// Business Logic Layer implementation for Product operations.
/// Owns all business rule validation before delegating to <see cref="IProductDataService"/>.
/// </summary>
public class ProductService(
    IProductDataService dataService,
    ILogger<ProductService> logger) : IProductService
{
    private readonly IProductDataService _dataService = dataService;
    private readonly ILogger<ProductService> _logger = logger;

    /// <inheritdoc/>
    public Task<IEnumerable<Product>> GetAllAsync() =>
        _dataService.GetAllAsync();

    /// <inheritdoc/>
    public async Task<Product> GetByIdAsync(int id)
    {
        var product = await _dataService.GetByIdAsync(id);

        // Throw KeyNotFoundException so GlobalExceptionMiddleware maps it to 404
        return product is null ? throw new KeyNotFoundException($"Product with id {id} was not found.") : product;
    }

    /// <inheritdoc/>
    public async Task<Product> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _dataService.CreateAsync(product);
        _logger.LogInformation("Product {Id} '{Name}' created", created.Id, created.Name);
        return created;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(int id, UpdateProductDto dto)
    {
        // Validates existence — throws KeyNotFoundException if missing
        var product = await GetByIdAsync(id);

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _dataService.UpdateAsync(product);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        // Validates existence — throws KeyNotFoundException if missing
        var product = await GetByIdAsync(id);
        await _dataService.DeleteAsync(product);
        _logger.LogInformation("Product {Id} deleted", id);
    }

    /// <inheritdoc/>
    public async Task ReduceStockAsync(int id, StockAdjustDto dto)
    {
        // Business rule: quantity must be positive
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        // Business rule: product must exist
        var product = await GetByIdAsync(id);

        // Business rule: cannot reduce below zero
        if (product.Stock < dto.Quantity)
        {
            _logger.LogWarning(
                "Insufficient stock for product {Id}: available {Stock}, requested {Quantity}",
                id, product.Stock, dto.Quantity);
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {product.Stock}, requested reduction: {dto.Quantity}.");
        }

        await _dataService.AdjustStockAsync(id, -dto.Quantity);
        _logger.LogInformation("Stock reduced for product {Id} by {Quantity}", id, dto.Quantity);
    }

    /// <inheritdoc/>
    public async Task RestoreStockAsync(int id, StockAdjustDto dto)
    {
        // Business rule: quantity must be positive
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        // Business rule: product must exist before restoring stock
        await GetByIdAsync(id);

        await _dataService.AdjustStockAsync(id, dto.Quantity);
        _logger.LogInformation("Stock restored for product {Id} by {Quantity}", id, dto.Quantity);
    }
}
