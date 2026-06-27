using ChopChop.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TradeStakeItem> TradeStakeItems => Set<TradeStakeItem>();
    public DbSet<FarmPlot> FarmPlots => Set<FarmPlot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<PlayerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ScoreEntry>(e =>
        {
            e.HasIndex(s => new { s.Category, s.Score });
            e.HasIndex(s => s.UserId);
        });

        builder.Entity<InventoryItem>(e =>
        {
            e.HasIndex(i => new { i.UserId, i.ItemId }).IsUnique();
        });

        builder.Entity<EventLog>(e =>
        {
            e.HasIndex(l => new { l.Type, l.EntityId });
            e.HasIndex(l => l.UserId);
        });

        builder.Entity<Room>()
            .HasMany(r => r.Members)
            .WithOne(m => m.Room)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Room>()
            .HasMany(r => r.Plots)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RoomMember>(e =>
        {
            e.HasIndex(m => new { m.RoomId, m.UserId });
        });

        builder.Entity<Trade>()
            .HasMany(t => t.Stakes)
            .WithOne(s => s.Trade)
            .HasForeignKey(s => s.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Trade>(e =>
        {
            e.HasIndex(t => t.RoomId);
            e.HasIndex(t => new { t.InitiatorUserId, t.Status });
            e.HasIndex(t => new { t.ResponderUserId, t.Status });
        });

        builder.Entity<FarmPlot>(e =>
        {
            e.HasIndex(p => p.RoomId);
            e.HasIndex(p => p.OwnerUserId);
        });
    }
}
