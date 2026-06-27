using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Core;

namespace ChopChop.Online.Networking
{
	/// <summary>
	/// Thin async HTTP facade over the ChopChop.Api backend. Serializes with Newtonsoft.Json,
	/// attaches the bearer token when <c>authenticated</c> is true, and maps every outcome to an
	/// <see cref="ApiResult{T}"/> (it does not throw for HTTP/network errors).
	/// </summary>
	public interface IApiClient : IService
	{
		Task<ApiResult<TResponse>> GetAsync<TResponse>(
			string path, bool authenticated = false, CancellationToken ct = default);

		Task<ApiResult<TResponse>> PostAsync<TResponse>(
			string path, object body, bool authenticated = false, CancellationToken ct = default);

		Task<ApiResult<TResponse>> PutAsync<TResponse>(
			string path, object body, bool authenticated = false, CancellationToken ct = default);

		Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
			string path, bool authenticated = false, CancellationToken ct = default);
	}

	/// <summary>Supplies the current bearer token to the <see cref="IApiClient"/> without a hard
	/// dependency on the session implementation (breaks the client↔session cycle).</summary>
	public interface ITokenProvider
	{
		string CurrentAccessToken { get; }
	}
}
