using System;
using R3;
using UnityEngine;
using Zenject;

namespace ChopChop.Online.Auth
{
	/// <summary>
	/// Default <see cref="ISessionService"/>. Persists the session in <see cref="PlayerPrefs"/> so a
	/// returning player stays signed in until the token expires or they log out.
	///
	/// NOTE: PlayerPrefs is convenient but not secure storage. Swap the persistence calls for an
	/// encrypted/keystore-backed store before shipping if the threat model requires it.
	/// </summary>
	public sealed class SessionService : ISessionService, IInitializable, IDisposable
	{
		private const string KeyToken = "chopchop.session.token";
		private const string KeyExpiry = "chopchop.session.expiryUtc";
		private const string KeyUserId = "chopchop.session.userId";
		private const string KeyEmail = "chopchop.session.email";
		private const string KeyDisplay = "chopchop.session.displayName";

		private readonly ReactiveProperty<AuthStatus> _status = new(AuthStatus.SignedOut);
		private readonly ReactiveProperty<UserDto> _currentUser = new((UserDto)null);

		private string _accessToken;
		private DateTime _expiresUtc;

		public ReadOnlyReactiveProperty<AuthStatus> Status => _status;
		public ReadOnlyReactiveProperty<UserDto> CurrentUser => _currentUser;

		public bool IsSignedIn => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresUtc;

		public string CurrentAccessToken => IsSignedIn ? _accessToken : null;

		public void Initialize() => RestorePersistedSession();

		public void SetSession(AuthResponse response)
		{
			if (response == null || string.IsNullOrEmpty(response.AccessToken))
			{
				Clear();
				return;
			}

			_accessToken = response.AccessToken;
			_expiresUtc = response.ExpiresUtc;
			Persist(response);

			_currentUser.Value = response.User;
			_status.Value = AuthStatus.SignedIn;
		}

		public void Clear()
		{
			_accessToken = null;
			_expiresUtc = default;

			foreach (var key in new[] { KeyToken, KeyExpiry, KeyUserId, KeyEmail, KeyDisplay })
				PlayerPrefs.DeleteKey(key);
			PlayerPrefs.Save();

			_currentUser.Value = null;
			_status.Value = AuthStatus.SignedOut;
		}

		private void Persist(AuthResponse response)
		{
			PlayerPrefs.SetString(KeyToken, response.AccessToken);
			PlayerPrefs.SetString(KeyExpiry, response.ExpiresUtc.ToUniversalTime().ToString("o"));
			PlayerPrefs.SetString(KeyUserId, response.User?.Id ?? string.Empty);
			PlayerPrefs.SetString(KeyEmail, response.User?.Email ?? string.Empty);
			PlayerPrefs.SetString(KeyDisplay, response.User?.DisplayName ?? string.Empty);
			PlayerPrefs.Save();
		}

		private void RestorePersistedSession()
		{
			var token = PlayerPrefs.GetString(KeyToken, null);
			if (string.IsNullOrEmpty(token)) return;

			if (!DateTime.TryParse(PlayerPrefs.GetString(KeyExpiry, null), null,
				    System.Globalization.DateTimeStyles.AdjustToUniversal, out var expiry)
			    || DateTime.UtcNow >= expiry)
			{
				Clear();
				return;
			}

			_accessToken = token;
			_expiresUtc = expiry;
			_currentUser.Value = new UserDto
			{
				Id = PlayerPrefs.GetString(KeyUserId, string.Empty),
				Email = PlayerPrefs.GetString(KeyEmail, string.Empty),
				DisplayName = PlayerPrefs.GetString(KeyDisplay, string.Empty),
			};
			_status.Value = AuthStatus.SignedIn;
		}

		public void Dispose()
		{
			_status.Dispose();
			_currentUser.Dispose();
		}
	}
}
