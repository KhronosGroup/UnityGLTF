/*
 * Copyright(c) 2017-2018 Sketchfab Inc.
 * License: https://github.com/sketchfab/UnityGLTF/blob/master/LICENSE
 */

#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.Networking;

namespace Sketchfab
{
	public delegate void UpdateCallback();
	public delegate void TextRequestCallback(string response);
	public delegate void DataRequestCallback(byte[] response);
	public delegate void HeaderRequestCallback(Dictionary<string, string> response);
	public delegate void WebRequestCallback(UnityWebRequest request);
	public delegate void ProgressCallback(float progress);
	public delegate void RefreshCallback();

	class Utils
	{
		public static JSONNode JSONParse(string jsondata)
		{
			jsondata = jsondata.Replace("null", "\"null\"");
			return JSON.Parse(jsondata);
		}

		// Add data to texture conversion util
		public static string buildModelFetchUrl(string uid)
		{
			return SketchfabPlugin.Urls.modelEndPoint + "/" + uid;
		}

		public static string humanifySize(int size)
		{
			string suffix = "";
			float readable = size;
			if (size >= 1000000) // Megabyte
			{
				suffix = "M";
				readable = size / 1000000.0f;
			}
			else if (size >= 1000) // Kilobyte
			{
				suffix = "k";
				readable = size / 1000.0f;
			}

			readable = Mathf.Round(readable * 100) / 100;
			return readable + suffix;
		}

		public static string humanifyFileSize(int size)
		{
			string suffix = "B";
			float readable = size;
			if (size >= 1024 * 1024) // Megabyte
			{
				suffix = "MB";
				readable = size / (1024.0f * 1024.0f);
			}
			else if (size >= 1024) // Kilobyte
			{
				suffix = "KB";
				readable = size / 1024.0f;
			}

			readable = Mathf.Round(readable * 100) / 100;
			return readable + suffix;
		}
	}

	public class SketchfabAPI
	{
		// Exporter objects and scripts
		public static int REQUEST_TIMEOUT = 10;
		public static int REQUEST_LIMITS = 10;
		List<SketchfabRequest> _requests;
		List<SketchfabRequest> _waiting;

		public SketchfabAPI()
		{
			_requests = new List<SketchfabRequest>();
		}

		public void checkValidity()
		{
			if (_requests == null)
			{
				_requests = new List<SketchfabRequest>();
			}
		}
		/// <summary>
		/// Check all current requests
		/// Consume them if success
		/// Logs errors if they failed
		/// </summary>
		public void Update()
		{
			if (_requests == null || _requests.Count == 0)
				return;

			for(int i = 0; i< _requests.Count; ++i)
			{
				if (_requests[i] != null && _requests[i].isValid())
				{
					if (_requests[i].isDone())
					{
						if (_requests[i].success())
						{
							_requests[i].digest();
						}
						else
						{
							Debug.Log(_requests[i].getError());
						}

						_requests[i].dispose();
						_requests[i] = null;
					}
					else
					{
						_requests[i].getProgress();
					}
				}

			}

			// Remove disposed requests
			for (int i = _requests.Count - 1; i >= 0; --i)
			{
				if(_requests[i] == null)
				{
					_requests.RemoveAt(i);
				}
			}
		}

		public void dropRequest(ref SketchfabRequest request)
		{
			request.dispose();
			_requests[0].dispose();
			_requests.Clear();
			_requests.Remove(request);
			request = null;
		}

		// Instanciate requests (need to use limit and buffer)
		public void registerRequest(SketchfabRequest request)
		{
			checkValidity();
			_requests.Add(request);
			request.send();
		}
	}

	public class SketchfabRequest
	{
		UnityWebRequest _request;
		UpdateCallback _callback, _failedCallback;
		TextRequestCallback _textCallback, _failedTextCallback;
		DataRequestCallback _dataCallback;
		HeaderRequestCallback _headerCallback;
		WebRequestCallback _webRequestCallback;
		ProgressCallback _progressCallback;

		public SketchfabRequest(string url, Dictionary<string, string> headers=null)
		{
			_request = new UnityWebRequest(url);
			if(headers != null)
			{
				foreach (string key in headers.Keys)
				{
					_request.SetRequestHeader(key, headers[key]);
				}
			}
			_request.downloadHandler = new DownloadHandlerBuffer();
		}

		public SketchfabRequest(UnityWebRequest request)
		{
			_request = request;
		}

		public SketchfabRequest(string url, List<IMultipartFormSection> _multiPart)
		{
			_request = UnityWebRequest.Post(url, _multiPart);
		}

		public SketchfabRequest(string url, Dictionary<string, string> headers, List<IMultipartFormSection> _multiPart = null)
		{
			if (_multiPart != null)
				_request = UnityWebRequest.Post(url, _multiPart);
			else
				_request = UnityWebRequest.Get(url);

			foreach (string key in headers.Keys)
			{
				_request.SetRequestHeader(key, headers[key]);
			}
		}

		public void setCallback(UpdateCallback cb)
		{
			_callback = cb;
		}

		public void setCallback(TextRequestCallback cb)
		{
			_textCallback = cb;
		}

		public void setFailedCallback(UpdateCallback cb)
		{
			_failedCallback = cb;
		}

		public void setFailedCallback(TextRequestCallback cb)
		{
			_failedTextCallback = cb;
		}

		public void setCallback(DataRequestCallback cb)
		{
			_dataCallback = cb;
			if(_request.downloadHandler == null)
			{
				_request.downloadHandler = new DownloadHandlerBuffer();
			}
		}

		public void setCallback(HeaderRequestCallback cb)
		{
			_headerCallback = cb;
		}

		public void setCallback(WebRequestCallback cb)
		{
			_webRequestCallback = cb;
		}

		public void send()
		{
			if (_request != null)
#if UNITY_5_6 || UNITY_2017_0 || UNITY_2017_1
				_request.Send();
#else
				_request.SendWebRequest();
#endif

		}

		public void digest()
		{
			if (_callback != null)
				_callback();
			if (_textCallback != null)
				_textCallback(_request.downloadHandler.text);
			if (_dataCallback != null)
				_dataCallback(_request.downloadHandler.data);
			if (_headerCallback != null)
				_headerCallback(_request.GetResponseHeaders());
			if (_webRequestCallback != null)
				_webRequestCallback(_request);
		}

		public  void setProgressCallback(ProgressCallback callback)
		{
			_progressCallback = callback;
		}

		public void dispose()
		{
			if(_request != null)
			{
				_request.Dispose();
				_request = null;
			}
		}

		public bool success()
		{
			return _request.responseCode == 200 || _request.responseCode == 201;
		}

		public string getError()
		{
			if (_failedCallback != null)
				_failedCallback();

			if (_failedTextCallback != null)
				_failedTextCallback(_request.downloadHandler.text);

			return _request.responseCode + " " + _request.error;
		}

		public bool isDone()
		{
			return _request.isDone;
		}

		public bool isValid()
		{
			return _request != null;
		}

		public string getResponse()
		{
			return _request.downloadHandler.text;
		}

		public byte[] getResponseData()
		{
			return _request.downloadHandler.data;
		}

		public void getProgress()
		{
			if(_progressCallback != null)
			{
				if (_request.uploadHandler != null)
					_progressCallback(_request.uploadProgress);
				else if (!_request.downloadHandler.isDone)
					_progressCallback(_request.downloadProgress);
			}
		}

		public Dictionary<string, string> getResponseHeaders()
		{
			return _request.GetResponseHeaders();
		}
	}
}
#endif
