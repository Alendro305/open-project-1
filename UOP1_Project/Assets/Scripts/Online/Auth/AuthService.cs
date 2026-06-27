using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Networking;

namespace ChopChop.Online.Auth
{
	public sealed class AuthService : IAuthService
	{
		private readonly IApiClient _api;
		private readonly ISessionService _session;

		public AuthService(IApiClient api, ISessionService session)
		{
			_api = api;
			_session = session;
		}

		public async Task<ApiResult<UserDto>> RegisterAsync(
			string email, string displayName, string password, CancellationToken ct)
		{
			var body = new RegisterRequest { Email = email, DisplayName = displayName, Password = password };
			var result = await _api.PostAsync<AuthResponse>("api/auth/register", body, authenticated: false, ct);
			return Apply(result);
		}

		public async Task<ApiResult<UserDto>> LoginAsync(
			string email, string password, CancellationToken ct)
		{
			var body = new LoginRequest { Email = email, Password = password };
			var result = await _api.PostAsync<AuthResponse>("api/auth/login", body, authenticated: false, ct);
			return Apply(result);
		}

		public void Logout() => _session.Clear();

		private ApiResult<UserDto> Apply(ApiResult<AuthResponse> result)
		{
			if (!result.IsSuccess)
				return ApiResult<UserDto>.Fail(result.Error, result.StatusCode);

			_session.SetSession(result.Value);
			return ApiResult<UserDto>.Ok(result.Value.User, result.StatusCode);
		}
	}
}
