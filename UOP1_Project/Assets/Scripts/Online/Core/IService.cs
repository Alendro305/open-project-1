namespace ChopChop.Online.Core
{
	/// <summary>
	/// Marker for the "S" in the SCV (Service / Controller / View) pattern.
	///
	/// A <b>Service</b> owns business logic and side effects (networking, audio, persistence …).
	/// It exposes plain async methods and/or R3 observables and knows nothing about the UI.
	/// Services are singletons bound in a Zenject installer and injected into Controllers.
	///
	/// A <b>Controller</b> (see <see cref="ScvController"/>) is the bridge: it holds the reactive
	/// state (<c>ReactiveProperty</c>) and intent (<c>ReactiveCommand</c>) for one screen and
	/// orchestrates Services. It contains no UnityEngine UI references.
	///
	/// A <b>View</b> (see <see cref="ScvView{TController}"/>) is a MonoBehaviour that binds visual
	/// elements to the Controller's reactive members. It contains no business logic.
	/// </summary>
	public interface IService
	{
	}
}
