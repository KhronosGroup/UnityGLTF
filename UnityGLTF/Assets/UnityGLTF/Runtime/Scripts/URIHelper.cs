using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class URIHelper
{
	private const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

	/// <summary>
	///  Get the absolute path to a gltf uri reference.
	/// </summary>
	/// <param name="uri">The path to the gltf file</param>
	/// <returns>A path without the filename or extension</returns>
	public static string AbsoluteUriPath(Uri uri)
	{
		var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length);
		return partialPath;
	}

	public static string GetFileFromUri(Uri uri)
	{
		return uri.Segments[uri.Segments.Length - 1];
	}

	/// <summary>
	/// Gets a directory name from a file path
	/// </summary>
	/// <param name="fullPath">Full path of a file</param>
	/// <returns>The name of directory file is in</returns>
	public static string GetDirectoryName(string fullPath)
	{
		var fileName = Path.GetFileName(fullPath);
		return fullPath.Substring(0, fullPath.Length - fileName.Length);
	}

	/// <summary>
	/// Tries to parse the uri as a base 64 encoded string
	/// </summary>
	/// <param name="uri">The string that represents the data</param>
	/// <param name="bufferData">Returns the deencoded bytes</param>
	public static void TryParseBase64(string uri, out byte[] bufferData)
	{
		Regex regex = new Regex(Base64StringInitializer);
		Match match = regex.Match(uri);
		bufferData = null;
		if (match.Success)
		{
			var base64Data = uri.Substring(match.Length);
			bufferData = Convert.FromBase64String(base64Data);
		}
	}

	/// <summary>
	/// Returns whether the input uri is base64 encoded
	/// </summary>
	/// <param name="uri">The uri data</param>
	/// <returns>Whether the uri is base64</returns>
	public static bool IsBase64Uri(string uri)
	{
		Regex regex = new Regex(Base64StringInitializer);
		Match match = regex.Match(uri);
		return match.Success;
	}
}
