using System.ComponentModel.DataAnnotations;

namespace ProductsApi.DTOs;

/// <summary>
/// Payload for creating a new product.
/// </summary>
public class CreateProductDto
{
    /// <summary>Product display name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Unit price. Must be greater than zero.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    /// <summary>Initial stock quantity. Must be zero or more.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }
}
