using System;

namespace ChopChop.Online.Auth
{
	// Client-side mirror of ChopChop.Api/Contracts. Field names are camelCased on the wire by the
	// Newtonsoft settings in HttpApiClient, matching ASP.NET's default JSON output.

	[Serializable]
	public sealed class RegisterRequest
	{
		public string Email;
		public string DisplayName;
		public string Password;
	}

	[Serializable]
	public sealed class LoginRequest
	{
		public string Email;
		public string Password;
	}

	[Serializable]
	public sealed class UserDto
	{
		public string Id;
		public string Email;
		public string DisplayName;
	}

	[Serializable]
	public sealed class AuthResponse
	{
		public string AccessToken;
		public DateTime ExpiresUtc;
		public UserDto User;
	}
}
