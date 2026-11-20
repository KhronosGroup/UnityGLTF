using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityGLTF
{
	/// <summary>
	/// Import dependencies on the remap targets (externalObjects) of a GLTFImporter, e.g. materials from a material
	/// library glTF. A remap target has to be imported before the file that uses it: on a clean import the remap was
	/// silently dropped (#914), and when the target changed while Unity was closed, the import failed with
	/// "Opening file VirtualArtifacts/Primary/...: file not found" (#919).
	///
	/// WORKAROUND: Unity only takes the import order from GatherDependenciesFromSourceFile, which gets nothing but the
	/// asset path. AssetImporter.GetAtPath returns null there, and Unity has no API (public or internal) to read the
	/// remaps of a path, so <see cref="FromMetaFile"/> reads them from the .meta file as text. That depends on the text
	/// format of .meta files, which Unity doesn't document or guarantee. Replace this class as soon as Unity can read
	/// the remaps of a path before the import, or declares remap targets of a ScriptedImporter as import dependencies
	/// itself. MaterialRemapImportTests checks that both methods still find the same targets, and
	/// <see cref="DeclareForImport"/> warns during the import when they don't.
	/// </summary>
	internal static class ExternalObjectDependencies
	{
		private static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})");

		internal const string NotInMetaFileWarning = "were not found in the .meta file";

		/// <summary>
		/// For OnImportAsset: re-import the asset when a remap target changes. GatherDependenciesFromSourceFile
		/// (<see cref="FromMetaFile"/>) already made sure the targets were imported first.
		/// Warns when the .meta workaround misses a target, so a change of the .meta format doesn't go unnoticed.
		/// </summary>
		public static void DeclareForImport(AssetImportContext ctx, AssetImporter importer)
		{
			var dependencies = FromImporter(importer);
			foreach (var dependencyPath in dependencies)
				ctx.DependsOnArtifact(dependencyPath);

			var missing = NotInMetaFile(dependencies, ctx.assetPath);
			if (missing.Count > 0)
				ctx.LogImportWarning($"The remap targets {string.Join(", ", missing)} {NotInMetaFileWarning}, so they may be imported after this file " +
				                     "and its remapped materials or textures can be missing or fail to load (#919). Please report this to UnityGLTF.");
		}

		internal static List<string> NotInMetaFile(List<string> dependencies, string assetPath)
		{
			if (dependencies.Count == 0) return dependencies;
			var fromMetaFile = FromMetaFile(assetPath);
			return dependencies.Where(d => !fromMetaFile.Contains(d)).ToList();
		}

		/// <summary>
		/// Asset paths of the remap targets in the .meta file of the asset, for GatherDependenciesFromSourceFile.
		/// The list looks like this:
		/// <code>
		///   externalObjects:
		///   - first:
		///       type: UnityEngine:Material
		///       assembly: UnityEngine.CoreModule
		///       name: MaterialName
		///     second: {fileID: 2100000, guid: 0123456789abcdef0123456789abcdef, type: 2}
		/// </code>
		/// </summary>
		public static List<string> FromMetaFile(string assetPath)
		{
			var dependencies = new List<string>();
			try
			{
				var metaPath = LongPath.ToLongPath(assetPath + ".meta");
				if (!File.Exists(metaPath)) return dependencies;

				var listIndent = -1;
				foreach (var line in File.ReadLines(metaPath))
				{
					var trimmed = line.TrimStart();
					if (trimmed.Length == 0) continue;
					var indent = line.Length - trimmed.Length;
					if (listIndent < 0)
					{
						if (trimmed.StartsWith("externalObjects:", StringComparison.Ordinal)) listIndent = indent;
						continue;
					}

					// the list ends at the next key on its level
					if (indent <= listIndent && !trimmed.StartsWith("-", StringComparison.Ordinal)) break;
					if (!trimmed.StartsWith("second:", StringComparison.Ordinal)) continue;

					var match = GuidRegex.Match(trimmed);
					if (match.Success) Add(dependencies, AssetDatabase.GUIDToAssetPath(match.Groups[1].Value), assetPath);
				}
			}
			catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
			{
				Debug.LogWarning($"Can't read the material and texture remaps of {assetPath} from its .meta file, they may be imported after it: {e.Message}");
			}
			return dependencies;
		}

		/// <summary>
		/// Asset paths of the remap targets of the importer. Doesn't load them.
		/// </summary>
		public static List<string> FromImporter(AssetImporter importer)
		{
			var dependencies = new List<string>();
			using (var serializedImporter = new SerializedObject(importer))
			{
				var externalObjects = serializedImporter.FindProperty("m_ExternalObjects");
				if (externalObjects == null) return dependencies;
				for (var i = 0; i < externalObjects.arraySize; i++)
				{
					var target = externalObjects.GetArrayElementAtIndex(i).FindPropertyRelative("second");
					if (target == null) continue;
#if UNITY_6000_4_OR_NEWER
					Add(dependencies, AssetDatabase.GetAssetPath(target.objectReferenceEntityIdValue), importer.assetPath);
#else
					Add(dependencies, AssetDatabase.GetAssetPath(target.objectReferenceInstanceIDValue), importer.assetPath);
#endif
				}
			}
			return dependencies;
		}

		private static void Add(List<string> dependencies, string dependencyPath, string assetPath)
		{
			if (string.IsNullOrEmpty(dependencyPath)) return;
			// a remap to the asset itself, or to a built-in resource such as "Resources/unity_builtin_extra", is no import dependency
			if (string.Equals(dependencyPath, assetPath, StringComparison.OrdinalIgnoreCase)) return;
			if (!dependencyPath.StartsWith("Assets/", StringComparison.Ordinal) && !dependencyPath.StartsWith("Packages/", StringComparison.Ordinal)) return;
			if (!dependencies.Contains(dependencyPath)) dependencies.Add(dependencyPath);
		}
	}
}
