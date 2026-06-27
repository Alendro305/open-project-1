using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using ChopChop.Online.Networking;
using R3;
using Zenject;

namespace ChopChop.Online.Trading
{
	/// <summary>
	/// Client service for peer-to-peer trading. Drives the server-authoritative trade state machine
	/// over REST and keeps <see cref="CurrentTrade"/> live from SignalR "TradeUpdated" pushes.
	/// <see cref="Invitations"/> fires when another player opens a trade with you.
	/// </summary>
	public interface ITradeService : IService
	{
		ReadOnlyReactiveProperty<TradeStateDto> CurrentTrade { get; }
		Observable<TradeStateDto> Invitations { get; }

		Task<ApiResult<TradeStateDto>> CreateAsync(string roomId, string responderUserId, CancellationToken ct = default);
		Task<ApiResult<TradeStateDto>> SetOfferAsync(int coins, IReadOnlyList<TradeStakeDto> items, CancellationToken ct = default);
		Task<ApiResult<TradeStateDto>> ConfirmAsync(CancellationToken ct = default);
		Task<ApiResult<TradeStateDto>> CancelAsync(CancellationToken ct = default);

		/// <summary>Adopt a trade (e.g. one received via <see cref="Invitations"/>) as the current trade.</summary>
		void SetCurrent(TradeStateDto trade);
	}

	public sealed class TradeService : ITradeService, IInitializable, IDisposable
	{
		private readonly IApiClient _api;
		private readonly IRealtimeService _realtime;
		private readonly ReactiveProperty<TradeStateDto> _current = new((TradeStateDto)null);
		private readonly CompositeDisposable _disposables = new();

		public TradeService(IApiClient api, IRealtimeService realtime)
		{
			_api = api;
			_realtime = realtime;
		}

		public ReadOnlyReactiveProperty<TradeStateDto> CurrentTrade => _current;
		public Observable<TradeStateDto> Invitations => _realtime.TradeInvited;

		public void Initialize()
		{
			_realtime.TradeUpdated
				.Subscribe(dto =>
				{
					if (_current.Value != null && dto.Id == _current.Value.Id)
						_current.Value = dto;
				})
				.AddTo(_disposables);
		}

		public void SetCurrent(TradeStateDto trade) => _current.Value = trade;

		public async Task<ApiResult<TradeStateDto>> CreateAsync(string roomId, string responderUserId, CancellationToken ct)
		{
			var result = await _api.PostAsync<TradeStateDto>("api/trades",
				new { roomId, responderUserId }, authenticated: true, ct);
			if (result.IsSuccess) _current.Value = result.Value;
			return result;
		}

		public async Task<ApiResult<TradeStateDto>> SetOfferAsync(int coins, IReadOnlyList<TradeStakeDto> items, CancellationToken ct)
		{
			if (_current.Value == null) return ApiResult<TradeStateDto>.Fail("No active trade.");
			var result = await _api.PutAsync<TradeStateDto>($"api/trades/{_current.Value.Id}/offer",
				new { coins, items }, authenticated: true, ct);
			if (result.IsSuccess) _current.Value = result.Value;
			return result;
		}

		public async Task<ApiResult<TradeStateDto>> ConfirmAsync(CancellationToken ct)
		{
			if (_current.Value == null) return ApiResult<TradeStateDto>.Fail("No active trade.");
			var result = await _api.PostAsync<TradeStateDto>($"api/trades/{_current.Value.Id}/confirm", null, authenticated: true, ct);
			if (result.IsSuccess) _current.Value = result.Value;
			return result;
		}

		public async Task<ApiResult<TradeStateDto>> CancelAsync(CancellationToken ct)
		{
			if (_current.Value == null) return ApiResult<TradeStateDto>.Fail("No active trade.");
			var result = await _api.PostAsync<TradeStateDto>($"api/trades/{_current.Value.Id}/cancel", null, authenticated: true, ct);
			if (result.IsSuccess) _current.Value = result.Value;
			return result;
		}

		public void Dispose()
		{
			_disposables.Dispose();
			_current.Dispose();
		}
	}
}
