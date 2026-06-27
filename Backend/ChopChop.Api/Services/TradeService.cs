using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using ChopChop.Api.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Services;

public interface ITradeService
{
    Task<Result<TradeStateDto>> CreateAsync(string initiatorUserId, string roomId, string responderUserId, CancellationToken ct = default);
    Task<Result<TradeStateDto>> SetOfferAsync(string userId, string tradeId, int coins, IReadOnlyList<TradeStakeDto> items, CancellationToken ct = default);
    Task<Result<TradeStateDto>> ConfirmAsync(string userId, string tradeId, CancellationToken ct = default);
    Task<Result<TradeStateDto>> CancelAsync(string userId, string tradeId, CancellationToken ct = default);
    Task<TradeStateDto?> GetStateAsync(string tradeId, CancellationToken ct = default);
}

public sealed class TradeService : ITradeService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly IRealtimeNotifier _notifier;

    public TradeService(AppDbContext db, IInventoryService inventory, IRealtimeNotifier notifier)
    {
        _db = db;
        _inventory = inventory;
        _notifier = notifier;
    }

    public async Task<Result<TradeStateDto>> CreateAsync(string initiatorUserId, string roomId, string responderUserId, CancellationToken ct)
    {
        if (initiatorUserId == responderUserId) return Result<TradeStateDto>.Fail("You can't trade with yourself.");

        var members = await _db.RoomMembers
            .Where(m => m.RoomId == roomId && m.LeftUtc == null && (m.UserId == initiatorUserId || m.UserId == responderUserId))
            .Select(m => m.UserId).Distinct().ToListAsync(ct);
        if (members.Count < 2) return Result<TradeStateDto>.Fail("Both players must be in the room.");

        var existing = await _db.Trades.FirstOrDefaultAsync(t => t.RoomId == roomId && t.Status == TradeStatus.Active &&
            ((t.InitiatorUserId == initiatorUserId && t.ResponderUserId == responderUserId) ||
             (t.InitiatorUserId == responderUserId && t.ResponderUserId == initiatorUserId)), ct);
        if (existing is not null) return Result<TradeStateDto>.Fail("A trade with this player is already open.");

        var trade = new Trade { RoomId = roomId, InitiatorUserId = initiatorUserId, ResponderUserId = responderUserId };
        _db.Trades.Add(trade);
        Audit.Log(_db, initiatorUserId, "trade.create", trade.Id, new { roomId, responderUserId });
        await _db.SaveChangesAsync(ct);

        var state = await GetStateAsync(trade.Id, ct)!;
        await _notifier.TradeUpdated(state!);
        await _notifier.TradeInvited(responderUserId, state!);
        return Result<TradeStateDto>.Success(state!);
    }

    public async Task<Result<TradeStateDto>> SetOfferAsync(string userId, string tradeId, int coins, IReadOnlyList<TradeStakeDto> items, CancellationToken ct)
    {
        var trade = await Load(tradeId, ct);
        if (trade is null || !IsParticipant(trade, userId)) return Result<TradeStateDto>.Fail("Trade not found.");
        if (trade.Status != TradeStatus.Active) return Result<TradeStateDto>.Fail("Trade is no longer active.");
        if (coins < 0) return Result<TradeStateDto>.Fail("Coins cannot be negative.");

        // Replace this side's stakes.
        var mine = trade.Stakes.Where(s => s.UserId == userId).ToList();
        _db.TradeStakeItems.RemoveRange(mine);
        foreach (var s in mine) trade.Stakes.Remove(s);

        foreach (var item in items ?? Array.Empty<TradeStakeDto>())
        {
            if (item.Quantity <= 0) continue;
            trade.Stakes.Add(new TradeStakeItem { TradeId = trade.Id, UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });
        }

        if (userId == trade.InitiatorUserId) trade.InitiatorCoins = coins;
        else trade.ResponderCoins = coins;

        // Any change to the offer invalidates both confirmations (standard trade safety).
        trade.InitiatorConfirmed = false;
        trade.ResponderConfirmed = false;
        trade.UpdatedUtc = DateTime.UtcNow;
        Audit.Log(_db, userId, "trade.offer", trade.Id, new { coins, items });
        await _db.SaveChangesAsync(ct);

        return await Broadcast(trade.Id, ct);
    }

    public async Task<Result<TradeStateDto>> ConfirmAsync(string userId, string tradeId, CancellationToken ct)
    {
        var trade = await Load(tradeId, ct);
        if (trade is null || !IsParticipant(trade, userId)) return Result<TradeStateDto>.Fail("Trade not found.");
        if (trade.Status != TradeStatus.Active) return Result<TradeStateDto>.Fail("Trade is no longer active.");

        if (userId == trade.InitiatorUserId) trade.InitiatorConfirmed = true;
        else trade.ResponderConfirmed = true;
        trade.UpdatedUtc = DateTime.UtcNow;
        Audit.Log(_db, userId, "trade.confirm", trade.Id, new { });
        await _db.SaveChangesAsync(ct);

        if (trade.InitiatorConfirmed && trade.ResponderConfirmed)
            return await ExecuteSwap(trade, ct);

        return await Broadcast(trade.Id, ct);
    }

    public async Task<Result<TradeStateDto>> CancelAsync(string userId, string tradeId, CancellationToken ct)
    {
        var trade = await Load(tradeId, ct);
        if (trade is null || !IsParticipant(trade, userId)) return Result<TradeStateDto>.Fail("Trade not found.");
        if (trade.Status == TradeStatus.Active)
        {
            trade.Status = TradeStatus.Cancelled;
            trade.UpdatedUtc = DateTime.UtcNow;
            Audit.Log(_db, userId, "trade.cancel", trade.Id, new { });
            await _db.SaveChangesAsync(ct);
        }
        return await Broadcast(trade.Id, ct);
    }

    private async Task<Result<TradeStateDto>> ExecuteSwap(Trade trade, CancellationToken ct)
    {
        var initiatorItems = trade.Stakes.Where(s => s.UserId == trade.InitiatorUserId).ToList();
        var responderItems = trade.Stakes.Where(s => s.UserId == trade.ResponderUserId).ToList();

        var initiatorProfile = await GetOrCreateProfile(trade.InitiatorUserId, ct);
        var responderProfile = await GetOrCreateProfile(trade.ResponderUserId, ct);

        // Validate funds/ownership BEFORE mutating anything.
        var error = await ValidateSide(trade.InitiatorUserId, trade.InitiatorCoins, initiatorProfile.Coins, initiatorItems, ct)
                 ?? await ValidateSide(trade.ResponderUserId, trade.ResponderCoins, responderProfile.Coins, responderItems, ct);
        if (error is not null)
        {
            // Roll the trade back to editable so players can fix their offer.
            trade.InitiatorConfirmed = false;
            trade.ResponderConfirmed = false;
            trade.UpdatedUtc = DateTime.UtcNow;
            Audit.Log(_db, string.Empty, "trade.swap_failed", trade.Id, new { error });
            await _db.SaveChangesAsync(ct);
            var failedState = await Broadcast(trade.Id, ct);
            return Result<TradeStateDto>.Fail(error);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Coins.
        initiatorProfile.Coins += trade.ResponderCoins - trade.InitiatorCoins;
        responderProfile.Coins += trade.InitiatorCoins - trade.ResponderCoins;

        // Items: each side's stakes move to the other side.
        foreach (var s in initiatorItems)
        {
            await _inventory.TryRemoveAsync(trade.InitiatorUserId, s.ItemId, s.Quantity, save: false, ct);
            await _inventory.AddAsync(trade.ResponderUserId, s.ItemId, s.Quantity, save: false, ct);
        }
        foreach (var s in responderItems)
        {
            await _inventory.TryRemoveAsync(trade.ResponderUserId, s.ItemId, s.Quantity, save: false, ct);
            await _inventory.AddAsync(trade.InitiatorUserId, s.ItemId, s.Quantity, save: false, ct);
        }

        trade.Status = TradeStatus.Completed;
        trade.UpdatedUtc = DateTime.UtcNow;
        Audit.Log(_db, string.Empty, "trade.complete", trade.Id, new
        {
            trade.InitiatorCoins,
            trade.ResponderCoins,
            initiatorItems = initiatorItems.Select(s => new { s.ItemId, s.Quantity }),
            responderItems = responderItems.Select(s => new { s.ItemId, s.Quantity }),
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await Broadcast(trade.Id, ct);
    }

    private async Task<string?> ValidateSide(string userId, int coins, int profileCoins, List<TradeStakeItem> items, CancellationToken ct)
    {
        if (coins > profileCoins) return "A player doesn't have enough coins.";
        foreach (var s in items)
            if (!await _inventory.HasAsync(userId, s.ItemId, s.Quantity, ct))
                return "A player no longer has the staked items.";
        return null;
    }

    private async Task<PlayerProfile> GetOrCreateProfile(string userId, CancellationToken ct)
    {
        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new PlayerProfile { UserId = userId };
            _db.PlayerProfiles.Add(profile);
        }
        return profile;
    }

    private Task<Trade?> Load(string tradeId, CancellationToken ct)
        => _db.Trades.Include(t => t.Stakes).FirstOrDefaultAsync(t => t.Id == tradeId, ct);

    private static bool IsParticipant(Trade t, string userId)
        => t.InitiatorUserId == userId || t.ResponderUserId == userId;

    private async Task<Result<TradeStateDto>> Broadcast(string tradeId, CancellationToken ct)
    {
        var state = await GetStateAsync(tradeId, ct);
        if (state is null) return Result<TradeStateDto>.Fail("Trade not found.");
        await _notifier.TradeUpdated(state);
        return Result<TradeStateDto>.Success(state);
    }

    public async Task<TradeStateDto?> GetStateAsync(string tradeId, CancellationToken ct)
    {
        var trade = await Load(tradeId, ct);
        if (trade is null) return null;

        TradeSideDto Side(string userId, int coins, bool confirmed) => new(
            userId, coins, confirmed,
            trade.Stakes.Where(s => s.UserId == userId).Select(s => new TradeStakeDto(s.ItemId, s.Quantity)).ToList());

        return new TradeStateDto(
            trade.Id, trade.RoomId, trade.Status.ToString(),
            Side(trade.InitiatorUserId, trade.InitiatorCoins, trade.InitiatorConfirmed),
            Side(trade.ResponderUserId, trade.ResponderCoins, trade.ResponderConfirmed));
    }
}
