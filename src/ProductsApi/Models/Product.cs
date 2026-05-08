namespace ProductsApi.Models;

/// <summary>
/// Represents a product in the inventory.
/// </summary>
public class Product
{
    /// <summary>Primary key (auto-incremented).</summary>
    public int Id { get; set; }

    /// <summary>Product display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unit price (decimal for monetary precision).</summary>
    public decimal Price { get; set; }

    /// <summary>Available stock quantity.</summary>
    public int Stock { get; set; }

    /// <summary>UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
