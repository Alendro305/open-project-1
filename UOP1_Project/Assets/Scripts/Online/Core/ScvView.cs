using R3;
using UnityEngine;
using Zenject;

namespace ChopChop.Online.Core
{
	/// <summary>
	/// Base class for the "V" in SCV. A view is a MonoBehaviour whose only job is to bind visual
	/// elements (uGUI Buttons, InputFields, Texts …) to its <typeparamref name="TController"/>'s
	/// reactive members. It holds no business logic.
	///
	/// The controller is injected by Zenject. Bindings are created in <see cref="Bind"/> and should
	/// be tied to <see cref="Bag"/> (or <c>.AddTo(this)</c>) so they are disposed when the GameObject
	/// is destroyed.
	/// </summary>
	public abstract class ScvView<TController> : MonoBehaviour where TController : class
	{
		/// <summary>Subscriptions owned by this view; disposed in <see cref="OnDestroy"/>.</summary>
		protected readonly CompositeDisposable Bag = new();

		protected TController Controller { get; private set; }

		[Inject]
		public void Construct(TController controller)
		{
			Controller = controller;
		}

		protected virtual void Start()
		{
			if (Controller == null)
			{
				Debug.LogError($"[{GetType().Name}] No controller was injected. " +
					"Ensure the GameObject is created through a Zenject context/factory.");
				return;
			}

			Bind(Controller);
		}

		/// <summary>Wire visual elements to the controller's reactive properties and commands.</summary>
		protected abstract void Bind(TController controller);

		protected virtual void OnDestroy()
		{
			Bag.Dispose();
		}
	}
}
