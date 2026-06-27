using ChopChop.Online.UI.Rooms;
using Zenject;

namespace ChopChop.Online.Installers
{
	/// <summary>
	/// Per-screen composition root for the room lobby. Attach to the SceneContext of the lobby scene
	/// (or a GameObjectContext on the lobby prefab). Binds <see cref="RoomController"/>; the app-wide
	/// IRoomService / IRealtimeService come from the parent ProjectContext (<see cref="OnlineInstaller"/>).
	/// </summary>
	public sealed class RoomScreenInstaller : MonoInstaller
	{
		public override void InstallBindings()
		{
			Container.BindInterfacesAndSelfTo<RoomController>().AsSingle();
		}
	}
}
