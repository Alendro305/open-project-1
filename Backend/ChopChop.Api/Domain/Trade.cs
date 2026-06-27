using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

public enum TradeStatus
{
    /// <summary>Both parties editing offers; not yet both confirmed.</summary>
    Active = 0,
    /// <summary>Both parties confirmed and ownership was swapped atomically.</summary>
    Completed = 1,
    /// <summary>Cancelled by a party or invalidated (e.g. a party left the room).</summary>
    Cancelled = 2,
}

/// <summary>
/// A server-authoritative peer-to-peer trade between two room members. Each side stakes coins and/or
/// items (<see cref="TradeStakeItem"/>) and confirms. When both have confirmed, the server validates
/// ownership and swaps everything in a single transaction. Any offer change clears both confirmations.
/// </summary>
public class Trade
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string RoomId { get; set; } = string.Empty;

    public string InitiatorUserId { get; set; } = string.Empty;
    public string ResponderUserId { get; set; } = string.Empty;

    public int InitiatorCoins { get; set; }
    public int ResponderCoins { get; set; }

    public bool InitiatorConfirmed { get; set; }
    public bool ResponderConfirmed { get; set; }

    public TradeStatus Status { get; set; } = TradeStatus.Active;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public List<TradeStakeItem> Stakes { get; set; } = new();
}

/// <summary>An item a participant has staked in a trade.</summary>
public class TradeStakeItem
{
    [Key]
    public int Id { get; set; }

    public string TradeId { get; set; } = string.Empty;
    public Trade? Trade { get; set; }

    /// <summary>Which side staked this (the initiator or the responder).</summary>
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
