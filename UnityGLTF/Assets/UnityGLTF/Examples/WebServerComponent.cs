using UnityEngine;

namespace UnityGLTF.Examples
{
	public class WebServerComponent : MonoBehaviour
	{
		private SimpleHTTPServer _server;

		void Start()
		{
			var appPath = Application.dataPath;
			var wwwPath = appPath.Substring(0, appPath.LastIndexOf("Assets")) + "www";
			_server = new SimpleHTTPServer(wwwPath, 8080);
			Debug.Log("Starting web server...");
		}

		private void OnDestroy()
		{
			_server.Stop();
		}
	}
}
