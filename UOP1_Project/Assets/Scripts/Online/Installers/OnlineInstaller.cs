using ChopChop.Online.Auth;
using ChopChop.Online.Config;
using ChopChop.Online.Farming;
using ChopChop.Online.Networking;
using ChopChop.Online.Rooms;
using ChopChop.Online.Trading;
using UnityEngine;
using Zenject;

namespace ChopChop.Online.Installers
{
	/// <summary>
	/// App-wide composition root for online functionality. Attach to the <b>ProjectContext</b>
	/// prefab (Assets/Resources/ProjectContext.prefab) so these services live for the whole session
	/// and are shared by every scene.
	///
	/// Bindings:
	///   BackendConfigSO  – designer config instance
	///   ISessionService  – session + token store (also ITokenProvider, IInitializable, IDisposable)
	///   IApiClient       – HttpClient facade
	///   IAuthService     – authentication business logic
	/// </summary>
	public sealed class OnlineInstaller : MonoInstaller
	{
		[SerializeField] private BackendConfigSO _backendConfig;

		public override void InstallBindings()
		{
			if (_backendConfig == null)
			{
				Debug.LogError("[OnlineInstaller] BackendConfig is not assigned. " +
					"Create one via Create → ChopChop → Online → Backend Config and assign it.");
				return;
			}

			Container.BindInstance(_backendConfig).AsSingle();

			// SessionService -> ISessionService + ITokenProvider + IInitializable + IDisposable.
			Container.BindInterfacesAndSelfTo<SessionService>().AsSingle();

			// HttpApiClient -> IApiClient (+ IDisposable). Depends on BackendConfigSO + ITokenProvider.
			Container.BindInterfacesAndSelfTo<HttpApiClient>().AsSingle();

			Container.BindInterfacesAndSelfTo<AuthService>().AsSingle();

			// Real-time + mechanics services (session-long; one shared hub connection).
			Container.BindInterfacesAndSelfTo<RealtimeService>().AsSingle();
			Container.BindInterfacesAndSelfTo<RoomService>().AsSingle();
			Container.BindInterfacesAndSelfTo<TradeService>().AsSingle();
			Container.BindInterfacesAndSelfTo<FarmService>().AsSingle();
		}
	}
}
