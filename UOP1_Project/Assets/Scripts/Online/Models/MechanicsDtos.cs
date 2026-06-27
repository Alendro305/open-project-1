using System.Collections.Generic;

namespace ChopChop.Online.Models
{
	// Client mirrors of ChopChop.Api/Contracts. Newtonsoft deserializes case-insensitively, so these
	// PascalCase members bind to the camelCase JSON from both REST responses and SignalR pushes.

	// ---------------------------------------------------------------- Inventory
	public sealed class InventoryItemDto
	{
		public string ItemId { get; set; }
		public int Quantity { get; set; }
	}

	// ---------------------------------------------------------------- Rooms
	public sealed class RoomSummaryDto
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string HostUserId { get; set; }
		public int MemberCount { get; set; }
		public int Capacity { get; set; }
	}

	public sealed class RoomMemberDto
	{
		public string UserId { get; set; }
		public string DisplayName { get; set; }
		public bool IsConnected { get; set; }
	}

	public sealed class RoomStateDto
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string HostUserId { get; set; }
		public int Capacity { get; set; }
		public List<RoomMemberDto> Members { get; set; } = new();
	}

	// ---------------------------------------------------------------- Trading
	public sealed class TradeStakeDto
	{
		public string ItemId { get; set; }
		public int Quantity { get; set; }
	}

	public sealed class TradeSideDto
	{
		public string UserId { get; set; }
		public int Coins { get; set; }
		public bool Confirmed { get; set; }
		public List<TradeStakeDto> Items { get; set; } = new();
	}

	public sealed class TradeStateDto
	{
		public string Id { get; set; }
		public string RoomId { get; set; }
		public string Status { get; set; } // "Active" | "Completed" | "Cancelled"
		public TradeSideDto Initiator { get; set; }
		public TradeSideDto Responder { get; set; }
	}

	// ---------------------------------------------------------------- Farming
	public sealed class FarmPlotDto
	{
		public string Id { get; set; }
		public string RoomId { get; set; }
		public string OwnerUserId { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public string State { get; set; } // "Empty" | "Planted" | "Growing" | "Harvestable"
		public string SeedItemId { get; set; }
		public string YieldItemId { get; set; }
		public int GrowSeconds { get; set; }
		public long? SecondsRemaining { get; set; }
		public int WaterCount { get; set; }
	}

	public sealed class SeedDefinitionDto
	{
		public string SeedItemId { get; set; }
		public string YieldItemId { get; set; }
		public int GrowSeconds { get; set; }
	}
}
