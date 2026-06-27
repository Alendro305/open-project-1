using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Services;

public interface IInventoryService
{
    Task<List<InventoryItem>> GetAsync(string userId, CancellationToken ct = default);

    /// <summary>Add (or stack) an item. Does not save unless <paramref name="save"/> is true.</summary>
    Task AddAsync(string userId, string itemId, int quantity, bool save = true, CancellationToken ct = default);

    /// <summary>True if the player currently holds at least <paramref name="quantity"/> of the item.</summary>
    Task<bool> HasAsync(string userId, string itemId, int quantity, CancellationToken ct = default);

    /// <summary>Remove items if the player has enough; returns false otherwise. Does not save unless requested.</summary>
    Task<bool> TryRemoveAsync(string userId, string itemId, int quantity, bool save = true, CancellationToken ct = default);
}

public sealed class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public Task<List<InventoryItem>> GetAsync(string userId, CancellationToken ct)
        => _db.InventoryItems.Where(i => i.UserId == userId && i.Quantity > 0).ToListAsync(ct);

    public async Task AddAsync(string userId, string itemId, int quantity, bool save, CancellationToken ct)
    {
        if (quantity <= 0) return;

        var row = await _db.InventoryItems.FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId, ct);
        if (row is null)
            _db.InventoryItems.Add(new InventoryItem { UserId = userId, ItemId = itemId, Quantity = quantity });
        else
            row.Quantity += quantity;

        Audit.Log(_db, userId, "inventory.add", itemId, new { itemId, quantity });
        if (save) await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasAsync(string userId, string itemId, int quantity, CancellationToken ct)
    {
        if (quantity <= 0) return true;
        var row = await _db.InventoryItems.FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId, ct);
        return row is not null && row.Quantity >= quantity;
    }

    public async Task<bool> TryRemoveAsync(string userId, string itemId, int quantity, bool save, CancellationToken ct)
    {
        if (quantity <= 0) return true;

        var row = await _db.InventoryItems.FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId, ct);
        if (row is null || row.Quantity < quantity) return false;

        row.Quantity -= quantity;
        Audit.Log(_db, userId, "inventory.remove", itemId, new { itemId, quantity });
        if (save) await _db.SaveChangesAsync(ct);
        return true;
    }
}
