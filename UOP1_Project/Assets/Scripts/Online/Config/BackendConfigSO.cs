using UnityEngine;

namespace ChopChop.Online.Config
{
	/// <summary>
	/// Designer-facing configuration for the online backend. Fits the project's existing
	/// ScriptableObject-config convention. Create one via
	/// <c>Assets → Create → ChopChop → Online → Backend Config</c> and reference it in the installer.
	/// </summary>
	[CreateAssetMenu(fileName = "BackendConfig", menuName = "ChopChop/Online/Backend Config")]
	public class BackendConfigSO : ScriptableObject
	{
		[Tooltip("Base URL of the ChopChop.Api server, including scheme and port. No trailing slash.")]
		[SerializeField] private string _baseUrl = "http://localhost:5080";

		[Tooltip("Per-request timeout in seconds.")]
		[SerializeField] private int _requestTimeoutSeconds = 20;

		public string BaseUrl => _baseUrl.TrimEnd('/');
		public int RequestTimeoutSeconds => Mathf.Max(1, _requestTimeoutSeconds);
	}
}
