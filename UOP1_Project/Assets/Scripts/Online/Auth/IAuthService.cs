using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;
using ChopChop.Online.Networking;

namespace ChopChop.Online.Auth
{
	/// <summary>
	/// Business logic for authentication. Talks to the backend via <see cref="IApiClient"/> and,
	/// on success, populates the <see cref="ISessionService"/>. Knows nothing about the UI.
	/// </summary>
	public interface IAuthService : IService
	{
		Task<ApiResult<UserDto>> RegisterAsync(
			string email, string displayName, string password, CancellationToken ct = default);

		Task<ApiResult<UserDto>> LoginAsync(
			string email, string password, CancellationToken ct = default);

		void Logout();
	}
}
