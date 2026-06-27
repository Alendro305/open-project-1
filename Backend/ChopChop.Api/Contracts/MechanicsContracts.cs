namespace ChopChop.Api.Contracts;

// ----------------------------------------------------------------- Inventory
public record InventoryItemDto(string ItemId, int Quantity);

// ----------------------------------------------------------------- Rooms
public record CreateRoomRequest(string Name, int Capacity);

public record RoomSummaryDto(string Id, string Name, string HostUserId, int MemberCount, int Capacity);

public record RoomMemberDto(string UserId, string DisplayName, bool IsConnected);

public record RoomStateDto(
    string Id, string Name, string HostUserId, int Capacity,
    IReadOnlyList<RoomMemberDto> Members);

// ----------------------------------------------------------------- Trading
public record CreateTradeRequest(string RoomId, string ResponderUserId);

public record TradeStakeDto(string ItemId, int Quantity);

public record SetOfferRequest(int Coins, IReadOnlyList<TradeStakeDto> Items);

public record TradeSideDto(string UserId, int Coins, bool Confirmed, IReadOnlyList<TradeStakeDto> Items);

public record TradeStateDto(
    string Id, string RoomId, string Status,
    TradeSideDto Initiator, TradeSideDto Responder);

// ----------------------------------------------------------------- Farming
public record PlacePlotRequest(string RoomId, float x, float y, float z);

public record PlantRequest(string SeedItemId);

public record FarmPlotDto(
    string Id, string RoomId, string OwnerUserId,
    float x, float y, float z,
    string State, string? SeedItemId, string? YieldItemId,
    int GrowSeconds, long? SecondsRemaining, int WaterCount);

public record SeedDefinitionDto(string SeedItemId, string YieldItemId, int GrowSeconds);
