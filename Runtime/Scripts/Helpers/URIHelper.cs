using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityGLTF
{
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

		private static readonly NormalizationForm[] UnicodeNormalizationForms = { NormalizationForm.FormC, NormalizationForm.FormD };

		/// <summary>
		/// The spellings of a glTF URI to try when looking for the file: as given, URI-unescaped,
		/// and both again in Unicode NFC and NFD form. glTF files written on macOS can contain decomposed (NFD)
		/// names while the file on disk is precomposed (NFC), e.g. after a git checkout on Windows,
		/// and Windows does not normalize file names.
		/// <para>
		/// Spec: glTF 2.0 "URIs / Relative Paths" says relative paths SHOULD undergo syntax-based normalization
		/// (RFC 3987, 5.3.2), where IRIs SHOULD be created in NFC. An NFD URI is valid but not normalized, and per the
		/// spec "its use may be unsupported", so loading it is optional support, not a requirement.
		/// It never changes the result for spec-conforming URIs: the exact spellings are tried first.
		/// Unescaping is required: per RFC 3987, 5.3.2.3 differences in percent-encoding MUST resolve to the same resource.
		/// Only canonical forms are used, compatibility forms (NFKC/NFKD) would match other files.
		/// </para>
		/// </summary>
		internal static List<string> GetUriPathVariants(string uri)
		{
			var variants = new List<string>(6) { uri };
			var unescaped = Uri.UnescapeDataString(uri);
			if (unescaped != uri) variants.Add(unescaped);

			for (int i = 0, count = variants.Count; i < count; i++)
			{
				foreach (var form in UnicodeNormalizationForms)
				{
					string normalized;
					try { normalized = variants[i].Normalize(form); }
					catch (ArgumentException) { continue; } // invalid Unicode, e.g. lone surrogates
					if (!variants.Contains(normalized)) variants.Add(normalized);
				}
			}
			return variants;
		}

		/// <summary>
		/// Combines <paramref name="rootDirectory"/> with each spelling of <paramref name="uri"/> (see <see cref="GetUriPathVariants"/>)
		/// and returns the first path that exists as a file, or null if none does.
		/// </summary>
		internal static string FindExistingFile(string rootDirectory, string uri, Func<string, string, string> combine)
		{
			foreach (var variant in GetUriPathVariants(uri))
			{
				var path = combine(rootDirectory, variant);
				if (File.Exists(LongPath.ToLongPath(path)))
					return path;
			}
			return null;
		}
	}
}
