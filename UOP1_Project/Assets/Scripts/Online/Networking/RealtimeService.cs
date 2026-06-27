using System;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Config;
using ChopChop.Online.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using R3;
using UnityEngine;

namespace ChopChop.Online.Networking
{
	/// <summary>
	/// SignalR-backed <see cref="IRealtimeService"/>. Uses the Newtonsoft JSON protocol (Unity-friendly)
	/// and forwards the JWT via <see cref="ITokenProvider"/>. Hub callbacks arrive on a background
	/// thread; observables are exposed via <c>ObserveOnMainThread()</c> so subscribers run on Unity's
	/// main thread.
	/// </summary>
	public sealed class RealtimeService : IRealtimeService, IDisposable
	{
		private readonly BackendConfigSO _config;
		private readonly ITokenProvider _tokens;

		private readonly Subject<RoomStateDto> _roomUpdated = new();
		private readonly Subject<PlotsUpdate> _plotsUpdated = new();
		private readonly Subject<TradeStateDto> _tradeUpdated = new();
		private readonly Subject<TradeStateDto> _tradeInvited = new();
		private readonly ReactiveProperty<bool> _isConnected = new(false);

		private HubConnection _connection;
		private readonly SemaphoreSlim _gate = new(1, 1);

		public RealtimeService(BackendConfigSO config, ITokenProvider tokens)
		{
			_config = config;
			_tokens = tokens;
		}

		public ReadOnlyReactiveProperty<bool> IsConnected => _isConnected;
		public Observable<RoomStateDto> RoomUpdated => _roomUpdated.ObserveOnMainThread();
		public Observable<PlotsUpdate> PlotsUpdated => _plotsUpdated.ObserveOnMainThread();
		public Observable<TradeStateDto> TradeUpdated => _tradeUpdated.ObserveOnMainThread();
		public Observable<TradeStateDto> TradeInvited => _tradeInvited.ObserveOnMainThread();

		public async Task ConnectAsync()
		{
			await _gate.WaitAsync();
			try
			{
				if (_connection is { State: HubConnectionState.Connected }) return;

				if (_connection == null)
				{
					_connection = BuildConnection();
					RegisterHandlers(_connection);
				}

				await _connection.StartAsync();
				_isConnected.Value = _connection.State == HubConnectionState.Connected;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[RealtimeService] Connect failed: {ex.Message}");
				_isConnected.Value = false;
			}
			finally
			{
				_gate.Release();
			}
		}

		public async Task DisconnectAsync()
		{
			await _gate.WaitAsync();
			try
			{
				if (_connection != null) await _connection.StopAsync();
				_isConnected.Value = false;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[RealtimeService] Disconnect failed: {ex.Message}");
			}
			finally
			{
				_gate.Release();
			}
		}

		public Task JoinRoomGroupAsync(string roomId) => InvokeSafe("JoinRoomGroup", roomId);
		public Task LeaveRoomGroupAsync(string roomId) => InvokeSafe("LeaveRoomGroup", roomId);

		private async Task InvokeSafe(string method, string arg)
		{
			if (_connection is not { State: HubConnectionState.Connected })
			{
				Debug.LogWarning($"[RealtimeService] Cannot invoke {method}; hub not connected.");
				return;
			}
			try { await _connection.InvokeAsync(method, arg); }
			catch (Exception ex) { Debug.LogWarning($"[RealtimeService] {method} failed: {ex.Message}"); }
		}

		private HubConnection BuildConnection()
		{
			var url = $"{_config.BaseUrl}/hub/game";
			return new HubConnectionBuilder()
				.WithUrl(url, options =>
				{
					options.AccessTokenProvider = () => Task.FromResult(_tokens.CurrentAccessToken);
				})
				.AddNewtonsoftJsonProtocol()
				.WithAutomaticReconnect()
				.Build();
		}

		private void RegisterHandlers(HubConnection c)
		{
			c.On<RoomStateDto>("RoomUpdated", dto => _roomUpdated.OnNext(dto));
			c.On<string, FarmPlotDto[]>("PlotsUpdated", (roomId, plots) => _plotsUpdated.OnNext(new PlotsUpdate(roomId, plots)));
			c.On<TradeStateDto>("TradeUpdated", dto => _tradeUpdated.OnNext(dto));
			c.On<TradeStateDto>("TradeInvited", dto => _tradeInvited.OnNext(dto));

			c.Reconnected += _ => { _isConnected.Value = true; return Task.CompletedTask; };
			c.Reconnecting += _ => { _isConnected.Value = false; return Task.CompletedTask; };
			c.Closed += _ => { _isConnected.Value = false; return Task.CompletedTask; };
		}

		public void Dispose()
		{
			_connection?.DisposeAsync();
			_roomUpdated.Dispose();
			_plotsUpdated.Dispose();
			_tradeUpdated.Dispose();
			_tradeInvited.Dispose();
			_isConnected.Dispose();
			_gate.Dispose();
		}
	}
}
