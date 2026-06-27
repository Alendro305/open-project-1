using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Auth;
using ChopChop.Online.Core;
using ChopChop.Online.Models;
using ChopChop.Online.Rooms;
using ChopChop.Online.Trading;
using R3;

namespace ChopChop.Online.UI.Trading
{
	/// <summary>
	/// SCV controller for a trade window. Projects the server-authoritative trade into "my side" /
	/// "their side" reactive views, exposes the local offer being edited, and the confirm/cancel/offer
	/// commands. Auto-adopts incoming trade invitations.
	/// </summary>
	public sealed class TradeController : ScvController
	{
		private readonly ITradeService _trades;
		private readonly ISessionService _session;
		private readonly IRoomService _rooms;

		// Local draft of my offer (the view edits these; "Update Offer" pushes them to the server).
		public ReactiveProperty<int> OfferedCoins { get; } = new(0);
		public ReactiveProperty<IReadOnlyList<TradeStakeDto>> OfferedItems { get; } = new(Array.Empty<TradeStakeDto>());

		private readonly ReactiveProperty<bool> _isBusy = new(false);
		private readonly ReactiveProperty<string> _status = new(string.Empty);

		public ReadOnlyReactiveProperty<bool> IsBusy => _isBusy;
		public ReadOnlyReactiveProperty<string> StatusMessage => _status;

		public ReadOnlyReactiveProperty<TradeStateDto> Trade => _trades.CurrentTrade;
		public ReadOnlyReactiveProperty<TradeSideDto> MySide { get; private set; }
		public ReadOnlyReactiveProperty<TradeSideDto> TheirSide { get; private set; }
		public ReadOnlyReactiveProperty<bool> IsActive { get; private set; }
		public ReadOnlyReactiveProperty<bool> IsCompleted { get; private set; }

		public ReactiveCommand<string> StartTradeCommand { get; } = new(); // arg: responder user id
		public ReactiveCommand<Unit> UpdateOfferCommand { get; } = new();
		public ReactiveCommand<Unit> ConfirmCommand { get; } = new();
		public ReactiveCommand<Unit> CancelCommand { get; } = new();

		public TradeController(ITradeService trades, ISessionService session, IRoomService rooms)
		{
			_trades = trades;
			_session = session;
			_rooms = rooms;
		}

		public override void Initialize()
		{
			Track(OfferedCoins); Track(OfferedItems); Track(_isBusy); Track(_status);
			Track(StartTradeCommand); Track(UpdateOfferCommand); Track(ConfirmCommand); Track(CancelCommand);

			MySide = Track(Trade.Select(SelectMine).ToReadOnlyReactiveProperty());
			TheirSide = Track(Trade.Select(SelectTheirs).ToReadOnlyReactiveProperty());
			IsActive = Track(Trade.Select(t => t != null && t.Status == "Active").ToReadOnlyReactiveProperty());
			IsCompleted = Track(Trade.Select(t => t != null && t.Status == "Completed").ToReadOnlyReactiveProperty());

			// Surface a completed/cancelled outcome as status text.
			Track(Trade.Subscribe(t =>
			{
				if (t == null) return;
				_status.Value = t.Status switch
				{
					"Completed" => "Trade complete!",
					"Cancelled" => "Trade cancelled.",
					_ => _status.Value,
				};
			}));

			// Accept invitations from other players.
			Track(_trades.Invitations.Subscribe(t =>
			{
				_trades.SetCurrent(t);
				_status.Value = "Trade request received.";
			}));

			Track(StartTradeCommand.SubscribeAwait((responderId, ct) => StartAsync(responderId, ct), AwaitOperation.Drop));
			Track(UpdateOfferCommand.SubscribeAwait((_, ct) => UpdateOfferAsync(ct), AwaitOperation.Drop));
			Track(ConfirmCommand.SubscribeAwait((_, ct) => RunAsync(_trades.ConfirmAsync(ct)), AwaitOperation.Drop));
			Track(CancelCommand.SubscribeAwait((_, ct) => RunAsync(_trades.CancelAsync(ct)), AwaitOperation.Drop));
		}

		private TradeSideDto SelectMine(TradeStateDto t)
		{
			if (t == null) return null;
			var me = _session.CurrentUser.CurrentValue?.Id;
			return t.Initiator?.UserId == me ? t.Initiator : t.Responder;
		}

		private TradeSideDto SelectTheirs(TradeStateDto t)
		{
			if (t == null) return null;
			var me = _session.CurrentUser.CurrentValue?.Id;
			return t.Initiator?.UserId == me ? t.Responder : t.Initiator;
		}

		private async ValueTask StartAsync(string responderId, CancellationToken ct)
		{
			_isBusy.Value = true;
			try
			{
				if (string.IsNullOrEmpty(responderId)) { _status.Value = "No player selected."; return; }
				var roomId = _rooms.CurrentRoom.CurrentValue?.Id;
				if (string.IsNullOrEmpty(roomId)) { _status.Value = "Join a room first."; return; }
				await RunAsync(_trades.CreateAsync(roomId, responderId, ct));
			}
			finally { _isBusy.Value = false; }
		}

		private async ValueTask UpdateOfferAsync(CancellationToken ct)
		{
			_isBusy.Value = true;
			try { await RunAsync(_trades.SetOfferAsync(OfferedCoins.Value, OfferedItems.Value, ct)); }
			finally { _isBusy.Value = false; }
		}

		private async ValueTask RunAsync(Task<Networking.ApiResult<TradeStateDto>> task)
		{
			var result = await task;
			if (!result.IsSuccess) _status.Value = result.Error;
		}
	}
}
