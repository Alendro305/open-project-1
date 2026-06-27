using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using ChopChop.Api.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Services;

public interface IFarmService
{
    Task<List<FarmPlotDto>> ListAsync(string roomId, CancellationToken ct = default);
    Task<Result<FarmPlotDto>> PlaceAsync(string userId, string roomId, float x, float y, float z, CancellationToken ct = default);
    Task<Result<FarmPlotDto>> PlantAsync(string userId, string plotId, string seedItemId, CancellationToken ct = default);
    Task<Result<FarmPlotDto>> WaterAsync(string userId, string plotId, CancellationToken ct = default);
    Task<Result<FarmPlotDto>> HarvestAsync(string userId, string plotId, CancellationToken ct = default);
    Task<Result<bool>> RemoveAsync(string userId, string plotId, CancellationToken ct = default);
}

/// <summary>Tiny success/failure envelope so services never throw for expected validation failures.</summary>
public readonly record struct Result<T>(bool Ok, string Error, T Value)
{
    public static Result<T> Success(T value) => new(true, string.Empty, value);
    public static Result<T> Fail(string error) => new(false, error, default!);
}

public sealed class FarmService : IFarmService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly ISeedCatalog _seeds;
    private readonly IRealtimeNotifier _notifier;

    public FarmService(AppDbContext db, IInventoryService inventory, ISeedCatalog seeds, IRealtimeNotifier notifier)
    {
        _db = db;
        _inventory = inventory;
        _seeds = seeds;
        _notifier = notifier;
    }

    public async Task<List<FarmPlotDto>> ListAsync(string roomId, CancellationToken ct)
    {
        var plots = await _db.FarmPlots.Where(p => p.RoomId == roomId).ToListAsync(ct);
        var now = DateTime.UtcNow;
        return plots.Select(p => ToDto(p, now)).ToList();
    }

    public async Task<Result<FarmPlotDto>> PlaceAsync(string userId, string roomId, float x, float y, float z, CancellationToken ct)
    {
        var inRoom = await _db.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.LeftUtc == null, ct);
        if (!inRoom) return Result<FarmPlotDto>.Fail("You must be in the room to place a plot.");

        var plot = new FarmPlot { RoomId = roomId, OwnerUserId = userId, PosX = x, PosY = y, PosZ = z, State = PlotState.Empty };
        _db.FarmPlots.Add(plot);
        Audit.Log(_db, userId, "plot.place", plot.Id, new { roomId, x, y, z });
        await _db.SaveChangesAsync(ct);

        await BroadcastRoom(roomId, ct);
        return Result<FarmPlotDto>.Success(ToDto(plot, DateTime.UtcNow));
    }

    public async Task<Result<FarmPlotDto>> PlantAsync(string userId, string plotId, string seedItemId, CancellationToken ct)
    {
        var plot = await OwnedPlot(userId, plotId, ct);
        if (plot is null) return Result<FarmPlotDto>.Fail("Plot not found or not yours.");
        if (plot.State != PlotState.Empty) return Result<FarmPlotDto>.Fail("Plot is not empty.");
        if (!_seeds.TryGet(seedItemId, out var def)) return Result<FarmPlotDto>.Fail("Unknown seed.");

        if (!await _inventory.TryRemoveAsync(userId, seedItemId, 1, save: false, ct))
            return Result<FarmPlotDto>.Fail("You don't have that seed.");

        plot.State = PlotState.Planted;
        plot.SeedItemId = def.SeedItemId;
        plot.YieldItemId = def.YieldItemId;
        plot.GrowSeconds = def.GrowSeconds;
        plot.PlantedUtc = DateTime.UtcNow;
        plot.GrowthStartUtc = null;
        plot.WaterCount = 0;
        Audit.Log(_db, userId, "plot.plant", plot.Id, new { seedItemId, def.GrowSeconds });
        await _db.SaveChangesAsync(ct);

        await BroadcastRoom(plot.RoomId, ct);
        return Result<FarmPlotDto>.Success(ToDto(plot, DateTime.UtcNow));
    }

    public async Task<Result<FarmPlotDto>> WaterAsync(string userId, string plotId, CancellationToken ct)
    {
        var plot = await OwnedPlot(userId, plotId, ct);
        if (plot is null) return Result<FarmPlotDto>.Fail("Plot not found or not yours.");
        if (plot.State is not (PlotState.Planted or PlotState.Growing))
            return Result<FarmPlotDto>.Fail("Nothing growing here to water.");

        var now = DateTime.UtcNow;
        plot.LastWateredUtc = now;
        plot.WaterCount++;
        if (plot.State == PlotState.Planted)
        {
            // First watering starts the (offline) grow timer.
            plot.State = PlotState.Growing;
            plot.GrowthStartUtc = now;
        }
        Audit.Log(_db, userId, "plot.water", plot.Id, new { plot.WaterCount });
        await _db.SaveChangesAsync(ct);

        await BroadcastRoom(plot.RoomId, ct);
        return Result<FarmPlotDto>.Success(ToDto(plot, now));
    }

    public async Task<Result<FarmPlotDto>> HarvestAsync(string userId, string plotId, CancellationToken ct)
    {
        var plot = await OwnedPlot(userId, plotId, ct);
        if (plot is null) return Result<FarmPlotDto>.Fail("Plot not found or not yours.");

        var now = DateTime.UtcNow;
        if (plot.ComputeState(now) != PlotState.Harvestable)
            return Result<FarmPlotDto>.Fail("Not ready to harvest yet.");

        var yieldId = plot.YieldItemId!;
        await _inventory.AddAsync(userId, yieldId, 1, save: false, ct);

        // Reset the plot to empty (the plant moved into the inventory).
        var roomId = plot.RoomId;
        plot.State = PlotState.Empty;
        plot.SeedItemId = null;
        plot.YieldItemId = null;
        plot.GrowSeconds = 0;
        plot.PlantedUtc = null;
        plot.GrowthStartUtc = null;
        plot.LastWateredUtc = null;
        plot.WaterCount = 0;
        Audit.Log(_db, userId, "plot.harvest", plot.Id, new { yieldItemId = yieldId });
        await _db.SaveChangesAsync(ct);

        await BroadcastRoom(roomId, ct);
        return Result<FarmPlotDto>.Success(ToDto(plot, now));
    }

    public async Task<Result<bool>> RemoveAsync(string userId, string plotId, CancellationToken ct)
    {
        var plot = await OwnedPlot(userId, plotId, ct);
        if (plot is null) return Result<bool>.Fail("Plot not found or not yours.");
        if (plot.State != PlotState.Empty) return Result<bool>.Fail("Clear the plot before removing it.");

        var roomId = plot.RoomId;
        _db.FarmPlots.Remove(plot);
        Audit.Log(_db, userId, "plot.remove", plotId, new { });
        await _db.SaveChangesAsync(ct);

        await BroadcastRoom(roomId, ct);
        return Result<bool>.Success(true);
    }

    private Task<FarmPlot?> OwnedPlot(string userId, string plotId, CancellationToken ct)
        => _db.FarmPlots.FirstOrDefaultAsync(p => p.Id == plotId && p.OwnerUserId == userId, ct);

    private async Task BroadcastRoom(string roomId, CancellationToken ct)
        => await _notifier.PlotsUpdated(roomId, await ListAsync(roomId, ct));

    private static FarmPlotDto ToDto(FarmPlot p, DateTime now)
    {
        var live = p.ComputeState(now);
        long? remaining = null;
        if (live == PlotState.Growing && p.GrowthStartUtc is { } start)
            remaining = Math.Max(0, (long)(start.AddSeconds(p.GrowSeconds) - now).TotalSeconds);
        else if (live == PlotState.Harvestable)
            remaining = 0;

        return new FarmPlotDto(
            p.Id, p.RoomId, p.OwnerUserId, p.PosX, p.PosY, p.PosZ,
            live.ToString(), p.SeedItemId, p.YieldItemId, p.GrowSeconds, remaining, p.WaterCount);
    }
}
