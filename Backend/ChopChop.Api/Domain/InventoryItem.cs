using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

/// <summary>
/// One stack of an item owned by a player. The item <b>catalog</b> (what items exist, their art,
/// names, etc.) lives in the Unity client as ItemSO; the backend only tracks ownership by
/// <see cref="ItemId"/> (the ItemSO id) and <see cref="Quantity"/>. Currency (coins) lives on
/// <see cref="PlayerProfile"/>, not here.
/// </summary>
public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Stable item identifier shared with the client's ItemSO catalog.</summary>
    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
