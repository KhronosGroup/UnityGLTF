using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	/// <summary>
	/// Remaps of imported materials and textures to other materials and textures of the same file, part of the
	/// GLTFImporter settings. These can't be external object remaps: those store a reference to the target, and Unity
	/// resolves a reference to an own sub-asset to null ("AssetImporter is referencing an asset from the previous
	/// import"); such a reference in the .meta file also caused #919. So they are stored by name and resolved against
	/// the new sub-assets on import.
	/// </summary>
	[Serializable]
	internal class RemapsInFile
	{
		[SerializeField] [NonReorderable] private List<Remap> materials = new List<Remap>();
		[SerializeField] [NonReorderable] private List<Remap> textures = new List<Remap>();

		[Serializable]
		private class Remap
		{
			public string source;
			public string target;
		}

		private List<Remap> Of(Type type) => typeof(Texture).IsAssignableFrom(type) ? textures : materials;

		internal int Count(Type type) => Of(type).Count;

		/// <summary>
		/// Name of the material or texture of the same file that <paramref name="source"/> is remapped to, or null.
		/// </summary>
		internal string GetTargetName(Object source)
		{
			if (!source) return null;
			return Of(source.GetType()).Find(x => x.source == source.name)?.target;
		}

		/// <summary>
		/// Remaps <paramref name="source"/> to the sub-asset named <paramref name="targetName"/>, null removes the remap.
		/// Returns whether anything changed.
		/// </summary>
		internal bool Set(Object source, string targetName)
		{
			if (!source) return false;
			var remaps = Of(source.GetType());
			var changed = remaps.RemoveAll(x => x.source == source.name) > 0;
			if (targetName == null || targetName == source.name) return changed;
			remaps.Add(new Remap { source = source.name, target = targetName });
			return true;
		}

		/// <summary>
		/// Removes all remaps of the type. Returns whether anything changed.
		/// </summary>
		internal bool Clear(Type type)
		{
			var remaps = Of(type);
			if (remaps.Count == 0) return false;
			remaps.Clear();
			return true;
		}

		/// <summary>
		/// The new sub-asset of the import that <paramref name="source"/> is remapped to, or null.
		/// </summary>
		internal T Resolve<T>(T source, Dictionary<string, T> subAssetsByName) where T : Object
		{
			var targetName = GetTargetName(source);
			if (targetName == null || targetName == source.name) return null;
			return subAssetsByName.TryGetValue(targetName, out var target) ? target : null;
		}

		internal static Dictionary<string, T> ByName<T>(IEnumerable<T> objects) where T : Object
		{
			// with duplicate names, the first one is the target, the same as for external object remaps (SourceAssetIdentifier)
			var byName = new Dictionary<string, T>();
			foreach (var o in objects)
				if (o && !byName.ContainsKey(o.name)) byName.Add(o.name, o);
			return byName;
		}
	}

	/// <summary>
	/// Remapping of imported materials and textures, to other assets (external object remaps) or to sub-assets of the
	/// same file (<see cref="RemapsInFile"/>).
	/// </summary>
	internal static class GLTFImporterRemaps
	{
		/// <summary>
		/// Remaps an imported material or texture of the file to <paramref name="target"/>. A target from the same file
		/// is stored by name (<see cref="RemapsInFile"/>), any other as external object remap.
		/// No target, or the source itself, removes the remap. Call <see cref="AssetImporter.SaveAndReimport"/> afterwards.
		/// </summary>
		internal static void SetRemap(this GLTFImporter importer, Object source, Object target)
		{
			if (!source) return;
			var id = new AssetImporter.SourceAssetIdentifier(source);
			importer.RemoveRemap(id);

			var isInFile = target && target != source && AssetDatabase.GetAssetPath(target) == importer.assetPath;
			if (target && target != source && !isInFile)
				importer.AddRemap(id, target);

			if (importer.RemapsInFile.Set(source, isInFile ? target.name : null))
				EditorUtility.SetDirty(importer);
		}

		/// <summary>
		/// Removes all remaps of the type, also the ones of sub-assets that don't exist anymore and the ones whose
		/// target is missing.
		/// </summary>
		internal static void RemoveAllRemaps(this GLTFImporter importer, Type type)
		{
			var externalRemaps = importer.GetExternalObjectMap()
				.Where(x => (x.Value && type.IsInstanceOfType(x.Value)) || (x.Key.type != null && type.IsAssignableFrom(x.Key.type)))
				.ToList();
			foreach (var remap in externalRemaps)
				importer.RemoveRemap(remap.Key);

			if (importer.RemapsInFile.Clear(type))
				EditorUtility.SetDirty(importer);
		}

		/// <summary>
		/// External object remaps of the type whose target can't be loaded: it was deleted, or it is a sub-asset of the
		/// same file (made before <see cref="RemapsInFile"/> existed). Unity always resolves those to null, so they never
		/// worked, and the reference to an own sub-asset in the .meta file is the one that caused #919.
		/// </summary>
		internal static List<AssetImporter.SourceAssetIdentifier> GetRemapsWithMissingTarget(this GLTFImporter importer, Type type) =>
			importer.GetExternalObjectMap()
				.Where(x => !x.Value && x.Key.type != null && type.IsAssignableFrom(x.Key.type))
				.Select(x => x.Key)
				.ToList();
	}
}
