using ProductsApi.DTOs;
using ProductsApi.Models;

namespace ProductsApi.BLL.Interfaces;

/// <summary>
/// Business Logic Layer contract for Product operations.
/// Enforces business rules before delegating persistence to the DAL.
/// </summary>
public interface IProductService
{
    /// <summary>Returns all products.</summary>
    Task<IEnumerable<Product>> GetAllAsync();

    /// <summary>
    /// Returns a product by Id.
    /// Throws <see cref="KeyNotFoundException"/> if not found.
    /// </summary>
    Task<Product> GetByIdAsync(int id);

    /// <summary>Creates a new product from the provided DTO.</summary>
    Task<Product> CreateAsync(CreateProductDto dto);

    /// <summary>
    /// Updates an existing product.
    /// Throws <see cref="KeyNotFoundException"/> if not found.
    /// </summary>
    Task UpdateAsync(int id, UpdateProductDto dto);

    /// <summary>
    /// Deletes a product.
    /// Throws <see cref="KeyNotFoundException"/> if not found.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Reduces the product's stock by the given quantity.
    /// Throws <see cref="ArgumentException"/> if quantity is not positive.
    /// Throws <see cref="InvalidOperationException"/> if insufficient stock.
    /// Throws <see cref="KeyNotFoundException"/> if product not found.
    /// </summary>
    Task ReduceStockAsync(int id, StockAdjustDto dto);

    /// <summary>
    /// Restores (increases) the product's stock by the given quantity.
    /// Throws <see cref="ArgumentException"/> if quantity is not positive.
    /// Throws <see cref="KeyNotFoundException"/> if product not found.
    /// </summary>
    Task RestoreStockAsync(int id, StockAdjustDto dto);
}
