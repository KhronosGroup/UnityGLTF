using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class URIHelper
{
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
}
