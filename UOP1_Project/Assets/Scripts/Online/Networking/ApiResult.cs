using System.Net;

namespace ChopChop.Online.Networking
{
	/// <summary>
	/// Outcome of an API call. Never throws for expected HTTP failures (4xx/5xx, network down);
	/// instead returns <see cref="IsSuccess"/> = false with a human-readable <see cref="Error"/>.
	/// </summary>
	public readonly struct ApiResult<T>
	{
		public bool IsSuccess { get; }
		public T Value { get; }
		public string Error { get; }
		public HttpStatusCode StatusCode { get; }

		private ApiResult(bool ok, T value, string error, HttpStatusCode status)
		{
			IsSuccess = ok;
			Value = value;
			Error = error;
			StatusCode = status;
		}

		public static ApiResult<T> Ok(T value, HttpStatusCode status = HttpStatusCode.OK)
			=> new(true, value, null, status);

		public static ApiResult<T> Fail(string error, HttpStatusCode status = 0)
			=> new(false, default, error, status);
	}
}
