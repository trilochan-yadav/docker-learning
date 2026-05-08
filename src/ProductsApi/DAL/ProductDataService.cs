using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductsApi.DAL.Interfaces;
using ProductsApi.Data;
using ProductsApi.Models;

namespace ProductsApi.DAL;

/// <summary>
/// EF Core implementation of <see cref="IProductDataService"/>.
/// Responsible only for data persistence — no business rules here.
/// </summary>
public class ProductDataService(
    AppDbContext context,
    ILogger<ProductDataService> logger) : IProductDataService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<ProductDataService> _logger = logger;

    /// <inheritdoc/>
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        _logger.LogDebug("Querying all products");
        return await _context.Products.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Querying product {Id}", id);
        return await _context.Products.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<Product> CreateAsync(Product product)
    {
        _logger.LogDebug("Inserting product '{Name}' into database", product.Name);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Product product)
    {
        _logger.LogDebug("Updating product {Id} in database", product.Id);
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Product product)
    {
        _logger.LogDebug("Removing product {Id} from database", product.Id);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task AdjustStockAsync(int id, int delta)
    {
        _logger.LogDebug("Applying stock delta {Delta} to product {Id}", delta, id);
        // Reload the entity so we have the latest tracked instance
        var product = await _context.Products.FindAsync(id);
        if (product is null) return;

        product.Stock += delta;
        await _context.SaveChangesAsync();
    }
}
