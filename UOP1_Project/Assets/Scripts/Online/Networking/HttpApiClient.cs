using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChopChop.Online.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace ChopChop.Online.Networking
{
	/// <summary>
	/// <see cref="HttpClient"/>-backed implementation of <see cref="IApiClient"/>.
	///
	/// Continuations resume on Unity's main thread because the UnitySynchronizationContext is
	/// installed there, so callers (controllers) can safely touch reactive state after awaiting.
	/// </summary>
	public sealed class HttpApiClient : IApiClient, IDisposable
	{
		private static readonly JsonSerializerSettings JsonSettings = new()
		{
			// camelCase on the wire, matching ASP.NET's default System.Text.Json output.
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			NullValueHandling = NullValueHandling.Ignore,
		};

		private readonly HttpClient _http;
		private readonly ITokenProvider _tokens;

		public HttpApiClient(BackendConfigSO config, ITokenProvider tokens)
		{
			_tokens = tokens;
			_http = new HttpClient
			{
				BaseAddress = new Uri(config.BaseUrl + "/"),
				Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds),
			};
			_http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		public Task<ApiResult<TResponse>> GetAsync<TResponse>(string path, bool authenticated, CancellationToken ct)
			=> SendAsync<TResponse>(HttpMethod.Get, path, null, authenticated, ct);

		public Task<ApiResult<TResponse>> PostAsync<TResponse>(string path, object body, bool authenticated, CancellationToken ct)
			=> SendAsync<TResponse>(HttpMethod.Post, path, body, authenticated, ct);

		public Task<ApiResult<TResponse>> PutAsync<TResponse>(string path, object body, bool authenticated, CancellationToken ct)
			=> SendAsync<TResponse>(HttpMethod.Put, path, body, authenticated, ct);

		public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string path, bool authenticated, CancellationToken ct)
			=> SendAsync<TResponse>(HttpMethod.Delete, path, null, authenticated, ct);

		private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
			HttpMethod method, string path, object body, bool authenticated, CancellationToken ct)
		{
			using var request = new HttpRequestMessage(method, path.TrimStart('/'));

			if (body != null)
			{
				var json = JsonConvert.SerializeObject(body, JsonSettings);
				request.Content = new StringContent(json, Encoding.UTF8, "application/json");
			}

			if (authenticated)
			{
				var token = _tokens.CurrentAccessToken;
				if (string.IsNullOrEmpty(token))
					return ApiResult<TResponse>.Fail("Not signed in.", HttpStatusCode.Unauthorized);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}

			HttpResponseMessage response;
			try
			{
				response = await _http.SendAsync(request, ct).ConfigureAwait(true);
			}
			catch (OperationCanceledException)
			{
				return ApiResult<TResponse>.Fail("Request cancelled.");
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[HttpApiClient] {method} {path} failed: {ex.Message}");
				return ApiResult<TResponse>.Fail("Could not reach the server. Check your connection.");
			}

			var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

			if (!response.IsSuccessStatusCode)
				return ApiResult<TResponse>.Fail(ExtractError(payload, response.StatusCode), response.StatusCode);

			try
			{
				var value = string.IsNullOrWhiteSpace(payload)
					? default
					: JsonConvert.DeserializeObject<TResponse>(payload, JsonSettings);
				return ApiResult<TResponse>.Ok(value, response.StatusCode);
			}
			catch (JsonException ex)
			{
				Debug.LogWarning($"[HttpApiClient] Failed to parse response from {path}: {ex.Message}");
				return ApiResult<TResponse>.Fail("Unexpected response from server.", response.StatusCode);
			}
		}

		/// <summary>Pull a friendly message out of the backend's <c>{ "error": "…" }</c> envelope.</summary>
		private static string ExtractError(string payload, HttpStatusCode status)
		{
			if (!string.IsNullOrWhiteSpace(payload))
			{
				try
				{
					var err = JsonConvert.DeserializeObject<ErrorEnvelope>(payload);
					if (!string.IsNullOrWhiteSpace(err?.Error)) return err.Error;
				}
				catch (JsonException) { /* fall through to status-based message */ }
			}

			return status switch
			{
				HttpStatusCode.Unauthorized => "Invalid credentials.",
				HttpStatusCode.Conflict => "That account already exists.",
				HttpStatusCode.BadRequest => "The request was rejected.",
				_ => $"Server error ({(int)status})."
			};
		}

		private sealed class ErrorEnvelope
		{
			public string Error { get; set; }
		}

		public void Dispose() => _http.Dispose();
	}
}
