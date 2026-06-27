using R3;
using ChopChop.Online.Core;
using ChopChop.Online.Networking;

namespace ChopChop.Online.Auth
{
	public enum AuthStatus
	{
		SignedOut,
		SignedIn,
	}

	/// <summary>
	/// Holds the authenticated session for the lifetime of the app and persists it across restarts.
	/// Also acts as the <see cref="ITokenProvider"/> consumed by <see cref="IApiClient"/>.
	/// </summary>
	public interface ISessionService : IService, ITokenProvider
	{
		ReadOnlyReactiveProperty<AuthStatus> Status { get; }
		ReadOnlyReactiveProperty<UserDto> CurrentUser { get; }

		/// <summary>True while a non-expired access token is held.</summary>
		bool IsSignedIn { get; }

		/// <summary>Store a freshly issued session (after register/login).</summary>
		void SetSession(AuthResponse response);

		/// <summary>Clear the session and wipe persisted credentials (logout).</summary>
		void Clear();
	}
}
