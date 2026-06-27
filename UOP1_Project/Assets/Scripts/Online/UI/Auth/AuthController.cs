using System;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Auth;
using ChopChop.Online.Core;
using R3;

namespace ChopChop.Online.UI.Auth
{
	public enum AuthMode
	{
		Login,
		Register,
	}

	/// <summary>
	/// Controller (the "C" in SCV) for the authentication screen. Owns all reactive state and the
	/// submit/toggle intent. The view binds to these members; the controller delegates the actual
	/// work to <see cref="IAuthService"/>. No UnityEngine.UI types appear here.
	/// </summary>
	public sealed class AuthController : ScvController
	{
		private readonly IAuthService _auth;

		// ---- Inputs (two-way: view writes user text in, reads back for restored/cleared values) ----
		public ReactiveProperty<string> Email { get; } = new(string.Empty);
		public ReactiveProperty<string> Password { get; } = new(string.Empty);
		public ReactiveProperty<string> DisplayName { get; } = new(string.Empty);
		public ReactiveProperty<AuthMode> Mode { get; } = new(AuthMode.Login);

		// ---- Outputs (one-way: controller -> view) ----
		private readonly ReactiveProperty<bool> _isBusy = new(false);
		private readonly ReactiveProperty<string> _statusMessage = new(string.Empty);

		public ReadOnlyReactiveProperty<bool> IsBusy => _isBusy;
		public ReadOnlyReactiveProperty<string> StatusMessage => _statusMessage;

		public ReadOnlyReactiveProperty<bool> IsRegisterMode { get; private set; }
		public ReadOnlyReactiveProperty<bool> CanSubmit { get; private set; }
		public ReadOnlyReactiveProperty<string> SubmitLabel { get; private set; }
		public ReadOnlyReactiveProperty<string> ToggleLabel { get; private set; }

		// ---- Intent ----
		public ReactiveCommand<Unit> SubmitCommand { get; } = new();
		public ReactiveCommand<Unit> ToggleModeCommand { get; } = new();

		/// <summary>Fires once the player is authenticated; the host screen listens to navigate away.</summary>
		public Observable<UserDto> SignedIn => _signedIn;
		private readonly Subject<UserDto> _signedIn = new();

		public AuthController(IAuthService auth)
		{
			_auth = auth;
		}

		public override void Initialize()
		{
			Track(Email); Track(Password); Track(DisplayName); Track(Mode);
			Track(_isBusy); Track(_statusMessage); Track(_signedIn);
			Track(SubmitCommand); Track(ToggleModeCommand);

			IsRegisterMode = Track(Mode.Select(m => m == AuthMode.Register).ToReadOnlyReactiveProperty());

			SubmitLabel = Track(Mode
				.Select(m => m == AuthMode.Register ? "Create Account" : "Sign In")
				.ToReadOnlyReactiveProperty());

			ToggleLabel = Track(Mode
				.Select(m => m == AuthMode.Register ? "Have an account? Sign in" : "New here? Create an account")
				.ToReadOnlyReactiveProperty());

			CanSubmit = Track(Observable
				.CombineLatest(Email, Password, _isBusy, (email, pwd, busy) =>
					!busy && LooksLikeEmail(email) && pwd != null && pwd.Length >= 6)
				.ToReadOnlyReactiveProperty());

			// Drop taps that arrive while a request is already in flight.
			Track(SubmitCommand.SubscribeAwait((_, ct) => SubmitAsync(ct), AwaitOperation.Drop));
			Track(ToggleModeCommand.Subscribe(_ => ToggleMode()));
		}

		private void ToggleMode()
		{
			Mode.Value = Mode.Value == AuthMode.Login ? AuthMode.Register : AuthMode.Login;
			_statusMessage.Value = string.Empty;
		}

		private async ValueTask SubmitAsync(CancellationToken ct)
		{
			_isBusy.Value = true;
			_statusMessage.Value = Mode.Value == AuthMode.Register ? "Creating account…" : "Signing in…";
			try
			{
				var result = Mode.Value == AuthMode.Register
					? await _auth.RegisterAsync(Email.Value.Trim(), DisplayName.Value.Trim(), Password.Value, ct)
					: await _auth.LoginAsync(Email.Value.Trim(), Password.Value, ct);

				if (result.IsSuccess)
				{
					_statusMessage.Value = $"Welcome, {result.Value.DisplayName}!";
					_signedIn.OnNext(result.Value);
				}
				else
				{
					_statusMessage.Value = result.Error;
				}
			}
			finally
			{
				_isBusy.Value = false;
			}
		}

		private static bool LooksLikeEmail(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			var at = value.IndexOf('@');
			return at > 0 && at < value.Length - 1 && value.IndexOf('.', at) > at;
		}
	}
}
