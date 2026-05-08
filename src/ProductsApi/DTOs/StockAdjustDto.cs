using System.ComponentModel.DataAnnotations;

namespace ProductsApi.DTOs;

/// <summary>
/// Payload for stock adjustment operations (reduce or restore).
/// </summary>
public class StockAdjustDto
{
    /// <summary>
    /// Number of units to reduce or restore. Must be greater than zero.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
