using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

public enum PlotState
{
    /// <summary>Placed but nothing planted.</summary>
    Empty = 0,
    /// <summary>Seed planted, awaiting first watering to begin growth.</summary>
    Planted = 1,
    /// <summary>Watered; growth timer running.</summary>
    Growing = 2,
    /// <summary>Grow time elapsed; ready to harvest with a spade.</summary>
    Harvestable = 3,
}

/// <summary>
/// A farm plot placed in a room's world by its owner. Growth is timestamp-based so it continues
/// while the player is offline: a plot becomes <see cref="PlotState.Harvestable"/> once
/// <c>GrowthStartUtc + GrowSeconds</c> has passed. The stored <see cref="State"/> is the last
/// persisted state; the live state is recomputed from the clock on read.
/// </summary>
public class FarmPlot
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string RoomId { get; set; } = string.Empty;
    public Room? Room { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    // World placement (room-local).
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public PlotState State { get; set; } = PlotState.Empty;

    public string? SeedItemId { get; set; }
    public string? YieldItemId { get; set; }
    public int GrowSeconds { get; set; }

    public DateTime? PlantedUtc { get; set; }
    /// <summary>Set on first watering; the grow timer counts from here.</summary>
    public DateTime? GrowthStartUtc { get; set; }
    public DateTime? LastWateredUtc { get; set; }
    public int WaterCount { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Live state computed from the clock (does not mutate the entity).</summary>
    public PlotState ComputeState(DateTime nowUtc)
    {
        if (State is PlotState.Empty or PlotState.Harvestable) return State;
        if (State == PlotState.Planted) return PlotState.Planted;
        // Growing: check whether the timer has elapsed.
        if (GrowthStartUtc is { } start && nowUtc >= start.AddSeconds(GrowSeconds))
            return PlotState.Harvestable;
        return PlotState.Growing;
    }
}
