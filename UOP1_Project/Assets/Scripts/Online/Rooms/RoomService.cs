using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using ChopChop.Online.Networking;
using R3;
using Zenject;

namespace ChopChop.Online.Rooms
{
	/// <summary>
	/// Client service for the room lobby. Wraps the REST endpoints and keeps <see cref="CurrentRoom"/>
	/// live by merging REST responses with SignalR "RoomUpdated" pushes. Opens the hub connection and
	/// joins/leaves the room's broadcast group as the player moves between rooms.
	/// </summary>
	public interface IRoomService : IService
	{
		ReadOnlyReactiveProperty<RoomStateDto> CurrentRoom { get; }

		Task<ApiResult<List<RoomSummaryDto>>> ListAsync(CancellationToken ct = default);
		Task<ApiResult<RoomStateDto>> CreateAsync(string name, int capacity, CancellationToken ct = default);
		Task<ApiResult<RoomStateDto>> JoinAsync(string roomId, CancellationToken ct = default);
		Task LeaveAsync(CancellationToken ct = default);
	}

	public sealed class RoomService : IRoomService, IInitializable, IDisposable
	{
		private readonly IApiClient _api;
		private readonly IRealtimeService _realtime;
		private readonly ReactiveProperty<RoomStateDto> _current = new((RoomStateDto)null);
		private readonly CompositeDisposable _disposables = new();

		public RoomService(IApiClient api, IRealtimeService realtime)
		{
			_api = api;
			_realtime = realtime;
		}

		public ReadOnlyReactiveProperty<RoomStateDto> CurrentRoom => _current;

		public void Initialize()
		{
			// Keep the current room fresh from server pushes (presence, members joining/leaving).
			_realtime.RoomUpdated
				.Subscribe(dto =>
				{
					if (_current.Value != null && dto.Id == _current.Value.Id)
						_current.Value = dto;
				})
				.AddTo(_disposables);
		}

		public Task<ApiResult<List<RoomSummaryDto>>> ListAsync(CancellationToken ct)
			=> _api.GetAsync<List<RoomSummaryDto>>("api/rooms", authenticated: true, ct);

		public async Task<ApiResult<RoomStateDto>> CreateAsync(string name, int capacity, CancellationToken ct)
		{
			var result = await _api.PostAsync<RoomStateDto>("api/rooms", new { name, capacity }, authenticated: true, ct);
			if (result.IsSuccess) await EnterRoom(result.Value);
			return result;
		}

		public async Task<ApiResult<RoomStateDto>> JoinAsync(string roomId, CancellationToken ct)
		{
			var result = await _api.PostAsync<RoomStateDto>($"api/rooms/{roomId}/join", null, authenticated: true, ct);
			if (result.IsSuccess) await EnterRoom(result.Value);
			return result;
		}

		public async Task LeaveAsync(CancellationToken ct)
		{
			var room = _current.Value;
			if (room == null) return;

			await _api.PostAsync<object>($"api/rooms/{room.Id}/leave", null, authenticated: true, ct);
			await _realtime.LeaveRoomGroupAsync(room.Id);
			_current.Value = null;
		}

		private async Task EnterRoom(RoomStateDto room)
		{
			_current.Value = room;
			await _realtime.ConnectAsync();
			await _realtime.JoinRoomGroupAsync(room.Id);
		}

		public void Dispose()
		{
			_disposables.Dispose();
			_current.Dispose();
		}
	}
}
