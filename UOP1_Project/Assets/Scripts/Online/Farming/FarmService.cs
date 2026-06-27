using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using ChopChop.Online.Networking;
using R3;
using Zenject;

namespace ChopChop.Online.Farming
{
	/// <summary>
	/// Client service for farming. Wraps the REST endpoints and keeps <see cref="Plots"/> for the
	/// active room live from SignalR "PlotsUpdated" pushes. Growth is computed server-side from
	/// timestamps, so plots advance even while the player is offline — a refresh on entering a room
	/// shows the up-to-date state.
	/// </summary>
	public interface IFarmService : IService
	{
		ReadOnlyReactiveProperty<IReadOnlyList<FarmPlotDto>> Plots { get; }

		Task<ApiResult<List<SeedDefinitionDto>>> GetSeedsAsync(CancellationToken ct = default);
		Task<ApiResult<List<FarmPlotDto>>> RefreshAsync(string roomId, CancellationToken ct = default);
		Task<ApiResult<FarmPlotDto>> PlaceAsync(string roomId, float x, float y, float z, CancellationToken ct = default);
		Task<ApiResult<FarmPlotDto>> PlantAsync(string plotId, string seedItemId, CancellationToken ct = default);
		Task<ApiResult<FarmPlotDto>> WaterAsync(string plotId, CancellationToken ct = default);
		Task<ApiResult<FarmPlotDto>> HarvestAsync(string plotId, CancellationToken ct = default);
		Task RemoveAsync(string plotId, CancellationToken ct = default);
	}

	public sealed class FarmService : IFarmService, IInitializable, IDisposable
	{
		private readonly IApiClient _api;
		private readonly IRealtimeService _realtime;
		private readonly ReactiveProperty<IReadOnlyList<FarmPlotDto>> _plots = new(Array.Empty<FarmPlotDto>());
		private readonly CompositeDisposable _disposables = new();

		private string _activeRoomId;

		public FarmService(IApiClient api, IRealtimeService realtime)
		{
			_api = api;
			_realtime = realtime;
		}

		public ReadOnlyReactiveProperty<IReadOnlyList<FarmPlotDto>> Plots => _plots;

		public void Initialize()
		{
			_realtime.PlotsUpdated
				.Subscribe(update =>
				{
					if (update.RoomId == _activeRoomId)
						_plots.Value = update.Plots ?? Array.Empty<FarmPlotDto>();
				})
				.AddTo(_disposables);
		}

		public Task<ApiResult<List<SeedDefinitionDto>>> GetSeedsAsync(CancellationToken ct)
			=> _api.GetAsync<List<SeedDefinitionDto>>("api/farm/seeds", authenticated: true, ct);

		public async Task<ApiResult<List<FarmPlotDto>>> RefreshAsync(string roomId, CancellationToken ct)
		{
			_activeRoomId = roomId;
			var result = await _api.GetAsync<List<FarmPlotDto>>($"api/farm/rooms/{roomId}/plots", authenticated: true, ct);
			if (result.IsSuccess) _plots.Value = result.Value;
			return result;
		}

		public Task<ApiResult<FarmPlotDto>> PlaceAsync(string roomId, float x, float y, float z, CancellationToken ct)
			=> _api.PostAsync<FarmPlotDto>($"api/farm/rooms/{roomId}/plots", new { x, y, z }, authenticated: true, ct);

		public Task<ApiResult<FarmPlotDto>> PlantAsync(string plotId, string seedItemId, CancellationToken ct)
			=> _api.PostAsync<FarmPlotDto>($"api/farm/plots/{plotId}/plant", new { seedItemId }, authenticated: true, ct);

		public Task<ApiResult<FarmPlotDto>> WaterAsync(string plotId, CancellationToken ct)
			=> _api.PostAsync<FarmPlotDto>($"api/farm/plots/{plotId}/water", null, authenticated: true, ct);

		public Task<ApiResult<FarmPlotDto>> HarvestAsync(string plotId, CancellationToken ct)
			=> _api.PostAsync<FarmPlotDto>($"api/farm/plots/{plotId}/harvest", null, authenticated: true, ct);

		public Task RemoveAsync(string plotId, CancellationToken ct)
			=> _api.DeleteAsync<object>($"api/farm/plots/{plotId}", authenticated: true, ct);

		public void Dispose()
		{
			_disposables.Dispose();
			_plots.Dispose();
		}
	}
}
