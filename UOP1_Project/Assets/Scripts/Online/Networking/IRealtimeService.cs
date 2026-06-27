using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using R3;

namespace ChopChop.Online.Networking
{
	/// <summary>Payload for the "PlotsUpdated" room broadcast.</summary>
	public readonly struct PlotsUpdate
	{
		public readonly string RoomId;
		public readonly FarmPlotDto[] Plots;
		public PlotsUpdate(string roomId, FarmPlotDto[] plots) { RoomId = roomId; Plots = plots; }
	}

	/// <summary>
	/// Real-time channel to the backend <c>GameHub</c> (SignalR). Surfaces server pushes as R3
	/// observables already marshalled onto Unity's main thread, so controllers can bind them directly.
	/// One connection is shared by every mechanic.
	/// </summary>
	public interface IRealtimeService : IService
	{
		ReadOnlyReactiveProperty<bool> IsConnected { get; }

		Observable<RoomStateDto> RoomUpdated { get; }
		Observable<PlotsUpdate> PlotsUpdated { get; }
		Observable<TradeStateDto> TradeUpdated { get; }
		Observable<TradeStateDto> TradeInvited { get; }

		/// <summary>Open the hub connection (no-op if already connected). Safe to call after sign-in.</summary>
		Task ConnectAsync();
		Task DisconnectAsync();

		/// <summary>Subscribe this connection to a room's broadcast group (presence + room/farm pushes).</summary>
		Task JoinRoomGroupAsync(string roomId);
		Task LeaveRoomGroupAsync(string roomId);
	}
}
