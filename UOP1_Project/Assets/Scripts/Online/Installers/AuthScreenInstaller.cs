using ChopChop.Online.UI.Auth;
using Zenject;

namespace ChopChop.Online.Installers
{
	/// <summary>
	/// Per-screen composition root for the Auth screen. Attach to the <b>SceneContext</b> of the auth
	/// scene (or a GameObjectContext on the auth prefab). Binds the <see cref="AuthController"/> so its
	/// <c>Initialize()</c> runs and the <see cref="AuthView"/> in the same context receives it.
	///
	/// The app-wide services (IAuthService, ISessionService …) are resolved from the parent
	/// ProjectContext installed by <see cref="OnlineInstaller"/>.
	/// </summary>
	public sealed class AuthScreenInstaller : MonoInstaller
	{
		public override void InstallBindings()
		{
			// AsSingle within this context; BindInterfacesAndSelfTo registers IInitializable so the
			// SceneKernel calls AuthController.Initialize() during context startup.
			Container.BindInterfacesAndSelfTo<AuthController>().AsSingle();
		}
	}
}
