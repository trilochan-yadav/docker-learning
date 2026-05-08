using System.ComponentModel.DataAnnotations;

namespace ProductsApi.DTOs;

/// <summary>
/// Payload for updating an existing product.
/// </summary>
public class UpdateProductDto
{
    /// <summary>New product display name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>New unit price. Must be greater than zero.</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    /// <summary>New stock quantity. Must be zero or more.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }
}
