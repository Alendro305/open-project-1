using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using ChopChop.Online.Rooms;
using R3;

namespace ChopChop.Online.UI.Rooms
{
	/// <summary>
	/// SCV controller for the room lobby shown after sign-in: list rooms, create one, join/leave.
	/// Reactive state + commands only; <see cref="IRoomService"/> does the work.
	/// </summary>
	public sealed class RoomController : ScvController
	{
		private readonly IRoomService _rooms;

		public ReactiveProperty<string> NewRoomName { get; } = new("My Room");
		public ReactiveProperty<int> NewRoomCapacity { get; } = new(8);

		private readonly ReactiveProperty<IReadOnlyList<RoomSummaryDto>> _available = new(Array.Empty<RoomSummaryDto>());
		private readonly ReactiveProperty<bool> _isBusy = new(false);
		private readonly ReactiveProperty<string> _status = new(string.Empty);

		public ReadOnlyReactiveProperty<IReadOnlyList<RoomSummaryDto>> AvailableRooms => _available;
		public ReadOnlyReactiveProperty<bool> IsBusy => _isBusy;
		public ReadOnlyReactiveProperty<string> StatusMessage => _status;

		public ReadOnlyReactiveProperty<RoomStateDto> CurrentRoom => _rooms.CurrentRoom;
		public ReadOnlyReactiveProperty<bool> IsInRoom { get; private set; }

		public ReactiveCommand<Unit> RefreshCommand { get; } = new();
		public ReactiveCommand<Unit> CreateRoomCommand { get; } = new();
		public ReactiveCommand<string> JoinRoomCommand { get; } = new();
		public ReactiveCommand<Unit> LeaveRoomCommand { get; } = new();

		/// <summary>Raised when the player has entered a room — host screen can move into gameplay.</summary>
		public Observable<RoomStateDto> EnteredRoom => _enteredRoom;
		private readonly Subject<RoomStateDto> _enteredRoom = new();

		public RoomController(IRoomService rooms)
		{
			_rooms = rooms;
		}

		public override void Initialize()
		{
			Track(NewRoomName); Track(NewRoomCapacity);
			Track(_available); Track(_isBusy); Track(_status); Track(_enteredRoom);
			Track(RefreshCommand); Track(CreateRoomCommand); Track(JoinRoomCommand); Track(LeaveRoomCommand);

			IsInRoom = Track(CurrentRoom.Select(r => r != null).ToReadOnlyReactiveProperty());

			Track(RefreshCommand.SubscribeAwait((_, ct) => RefreshAsync(ct), AwaitOperation.Drop));
			Track(CreateRoomCommand.SubscribeAwait((_, ct) => CreateAsync(ct), AwaitOperation.Drop));
			Track(JoinRoomCommand.SubscribeAwait((id, ct) => JoinAsync(id, ct), AwaitOperation.Drop));
			Track(LeaveRoomCommand.SubscribeAwait((_, ct) => LeaveAsync(ct), AwaitOperation.Drop));

			// Initial population.
			RefreshCommand.Execute(Unit.Default);
		}

		private async ValueTask RefreshAsync(CancellationToken ct)
		{
			_isBusy.Value = true;
			try
			{
				var result = await _rooms.ListAsync(ct);
				if (result.IsSuccess) _available.Value = result.Value;
				else _status.Value = result.Error;
			}
			finally { _isBusy.Value = false; }
		}

		private async ValueTask CreateAsync(CancellationToken ct)
		{
			_isBusy.Value = true;
			_status.Value = "Creating room…";
			try
			{
				var result = await _rooms.CreateAsync(NewRoomName.Value.Trim(), NewRoomCapacity.Value, ct);
				if (result.IsSuccess) { _status.Value = string.Empty; _enteredRoom.OnNext(result.Value); }
				else _status.Value = result.Error;
			}
			finally { _isBusy.Value = false; }
		}

		private async ValueTask JoinAsync(string roomId, CancellationToken ct)
		{
			_isBusy.Value = true;
			_status.Value = "Joining…";
			try
			{
				var result = await _rooms.JoinAsync(roomId, ct);
				if (result.IsSuccess) { _status.Value = string.Empty; _enteredRoom.OnNext(result.Value); }
				else _status.Value = result.Error;
			}
			finally { _isBusy.Value = false; }
		}

		private async ValueTask LeaveAsync(CancellationToken ct)
		{
			_isBusy.Value = true;
			try
			{
				await _rooms.LeaveAsync(ct);
				await RefreshAsync(ct);
			}
			finally { _isBusy.Value = false; }
		}
	}
}
