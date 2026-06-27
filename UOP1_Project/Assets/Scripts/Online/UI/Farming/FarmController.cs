using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Farming;
using ChopChop.Online.Models;
using ChopChop.Online.Rooms;
using R3;
using UnityEngine;

namespace ChopChop.Online.UI.Farming
{
	/// <summary>
	/// SCV controller for farming. Exposes the room's plots and the seed catalog reactively and the
	/// place/plant/water/harvest/remove commands. Plots refresh automatically when the player enters a
	/// room and stay live via SignalR pushes; growth is server-timed so it continues offline.
	/// </summary>
	public sealed class FarmController : ScvController
	{
		private readonly IFarmService _farm;
		private readonly IRoomService _rooms;

		private readonly ReactiveProperty<IReadOnlyList<SeedDefinitionDto>> _seeds = new(Array.Empty<SeedDefinitionDto>());
		private readonly ReactiveProperty<bool> _isBusy = new(false);
		private readonly ReactiveProperty<string> _status = new(string.Empty);

		public ReadOnlyReactiveProperty<IReadOnlyList<FarmPlotDto>> Plots => _farm.Plots;
		public ReadOnlyReactiveProperty<IReadOnlyList<SeedDefinitionDto>> Seeds => _seeds;
		public ReadOnlyReactiveProperty<bool> IsBusy => _isBusy;
		public ReadOnlyReactiveProperty<string> StatusMessage => _status;

		public ReactiveCommand<Vector3> PlacePlotCommand { get; } = new();
		public ReactiveCommand<(string plotId, string seedItemId)> PlantCommand { get; } = new();
		public ReactiveCommand<string> WaterCommand { get; } = new();
		public ReactiveCommand<string> HarvestCommand { get; } = new();
		public ReactiveCommand<string> RemoveCommand { get; } = new();

		public FarmController(IFarmService farm, IRoomService rooms)
		{
			_farm = farm;
			_rooms = rooms;
		}

		public override void Initialize()
		{
			Track(_seeds); Track(_isBusy); Track(_status);
			Track(PlacePlotCommand); Track(PlantCommand); Track(WaterCommand); Track(HarvestCommand); Track(RemoveCommand);

			// Refresh plots whenever we enter (or change) a room.
			Track(_rooms.CurrentRoom.Subscribe(room =>
			{
				if (room != null) RefreshSeedsAndPlots(room.Id);
			}));

			Track(PlacePlotCommand.SubscribeAwait((pos, ct) => PlaceAsync(pos, ct), AwaitOperation.Drop));
			Track(PlantCommand.SubscribeAwait((args, ct) => RunAsync(_farm.PlantAsync(args.plotId, args.seedItemId, ct)), AwaitOperation.Sequential));
			Track(WaterCommand.SubscribeAwait((plotId, ct) => RunAsync(_farm.WaterAsync(plotId, ct)), AwaitOperation.Sequential));
			Track(HarvestCommand.SubscribeAwait((plotId, ct) => RunAsync(_farm.HarvestAsync(plotId, ct)), AwaitOperation.Sequential));
			Track(RemoveCommand.SubscribeAwait(async (plotId, ct) =>
			{
				_isBusy.Value = true;
				try { await _farm.RemoveAsync(plotId, ct); }
				finally { _isBusy.Value = false; }
			}, AwaitOperation.Sequential));
		}

		private async void RefreshSeedsAndPlots(string roomId)
		{
			var seeds = await _farm.GetSeedsAsync(CancellationToken.None);
			if (seeds.IsSuccess) _seeds.Value = seeds.Value;
			await _farm.RefreshAsync(roomId, CancellationToken.None);
		}

		private async ValueTask PlaceAsync(Vector3 pos, CancellationToken ct)
		{
			var roomId = _rooms.CurrentRoom.CurrentValue?.Id;
			if (string.IsNullOrEmpty(roomId)) { _status.Value = "Join a room first."; return; }
			await RunAsync(_farm.PlaceAsync(roomId, pos.x, pos.y, pos.z, ct));
		}

		private async ValueTask RunAsync(Task<Networking.ApiResult<FarmPlotDto>> task)
		{
			_isBusy.Value = true;
			try
			{
				var result = await task;
				if (!result.IsSuccess) _status.Value = result.Error;
			}
			finally { _isBusy.Value = false; }
		}
	}
}
