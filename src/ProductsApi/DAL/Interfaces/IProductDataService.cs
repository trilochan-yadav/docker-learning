using ProductsApi.Models;

namespace ProductsApi.DAL.Interfaces;

/// <summary>
/// Data Access Layer contract for Product persistence.
/// Abstracts EF Core operations so the BLL never touches DbContext directly.
/// </summary>
public interface IProductDataService
{
    /// <summary>Returns all products.</summary>
    Task<IEnumerable<Product>> GetAllAsync();

    /// <summary>Returns a product by its primary key, or null if not found.</summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>Persists a new product and returns it with the generated Id.</summary>
    Task<Product> CreateAsync(Product product);

    /// <summary>Persists changes to an existing product entity.</summary>
    Task UpdateAsync(Product product);

    /// <summary>Removes the product entity from the database.</summary>
    Task DeleteAsync(Product product);

    /// <summary>
    /// Applies a stock delta (+/-) to the product with the given id.
    /// Assumes the caller (BLL) has already validated that the operation is safe.
    /// </summary>
    Task AdjustStockAsync(int id, int delta);
}
