using Microsoft.AspNetCore.Mvc;
using ProductsApi.BLL.Interfaces;
using ProductsApi.DTOs;

namespace ProductsApi.Controllers;

/// <summary>
/// REST controller for Product CRUD and stock management operations.
/// All endpoints require the X-Api-Key header (enforced by ApiKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController(
    IProductService service,
    ILogger<ProductsController> logger) : ControllerBase
{
    private readonly IProductService _service = service;
    private readonly ILogger<ProductsController> _logger = logger;

    // ── CRUD ──────────────────────────────────────────────────────────────────

    /// <summary>Returns all products.</summary>
    /// <response code="200">List of products.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _service.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} products", products.Count());
        return Ok(products);
    }

    /// <summary>Returns a single product by Id.</summary>
    /// <param name="id">Product primary key.</param>
    /// <response code="200">The requested product.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Retrieving product {Id}", id);
        var product = await _service.GetByIdAsync(id);
        return Ok(product);
    }

    /// <summary>Creates a new product.</summary>
    /// <param name="dto">Product creation payload.</param>
    /// <response code="201">Product created. Location header points to the new resource.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        _logger.LogInformation("Creating product {Name}", dto.Name);
        var product = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Updates an existing product.</summary>
    /// <param name="id">Product primary key.</param>
    /// <param name="dto">Product update payload.</param>
    /// <response code="204">Product updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        _logger.LogInformation("Updating product {Id}", id);
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>Deletes a product.</summary>
    /// <param name="id">Product primary key.</param>
    /// <response code="204">Product deleted successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deleting product {Id}", id);
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // ── Stock management ──────────────────────────────────────────────────────

    /// <summary>Reduces product stock by the specified quantity.</summary>
    /// <param name="id">Product primary key.</param>
    /// <param name="dto">Quantity to reduce.</param>
    /// <response code="204">Stock reduced successfully.</response>
    /// <response code="400">Invalid quantity or insufficient stock.</response>
    /// <response code="404">Product not found.</response>
    [HttpPatch("{id:int}/reduce-stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReduceStock(int id, [FromBody] StockAdjustDto dto)
    {
        _logger.LogInformation("Reducing stock for product {Id} by {Quantity}", id, dto.Quantity);
        await _service.ReduceStockAsync(id, dto);
        return NoContent();
    }

    /// <summary>Restores (increases) product stock by the specified quantity.</summary>
    /// <param name="id">Product primary key.</param>
    /// <param name="dto">Quantity to restore.</param>
    /// <response code="204">Stock restored successfully.</response>
    /// <response code="400">Invalid quantity.</response>
    /// <response code="404">Product not found.</response>
    [HttpPatch("{id:int}/restore-stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreStock(int id, [FromBody] StockAdjustDto dto)
    {
        _logger.LogInformation("Restoring stock for product {Id} by {Quantity}", id, dto.Quantity);
        await _service.RestoreStockAsync(id, dto);
        return NoContent();
    }
}
