using System;
using R3;
using Zenject;

namespace ChopChop.Online.Core
{
	/// <summary>
	/// Base class for the "C" in SCV. A controller is a plain (non-MonoBehaviour) object created
	/// and lifecycle-managed by Zenject. It exposes <see cref="ReactiveProperty{T}"/> state and
	/// <see cref="ReactiveCommand{T}"/> intent, and wires its subscriptions in <see cref="Initialize"/>.
	///
	/// Anything added to <see cref="Disposables"/> is torn down automatically when the container
	/// disposes the controller, so views never need to manage the controller's subscriptions.
	/// </summary>
	public abstract class ScvController : IInitializable, IDisposable
	{
		/// <summary>Bag for every subscription/reactive member owned by this controller.</summary>
		protected readonly CompositeDisposable Disposables = new();

		/// <summary>Called by Zenject after construction. Override to wire reactive pipelines.</summary>
		public virtual void Initialize() { }

		public virtual void Dispose()
		{
			Disposables.Dispose();
		}

		/// <summary>Convenience: register a disposable for automatic teardown.</summary>
		protected T Track<T>(T disposable) where T : IDisposable
		{
			Disposables.Add(disposable);
			return disposable;
		}
	}
}
