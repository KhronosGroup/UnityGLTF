using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace UnityGLTF.Extensions
{
	public class KHR_animation_pointer_Resolver
	{
		private readonly List<KHR_animation_pointer> registered = new List<KHR_animation_pointer>();
		private static readonly ProfilerMarker animationPointerResolverMarker = new ProfilerMarker("Resolve Animation Pointer");

		public void Add(KHR_animation_pointer anim)
		{
			registered.Add(anim);
		}

		// private struct MaterialMapping
		// {
		// 	public string propertyName;
		// 	public string exportName;
		// }
		//
		// private readonly Dictionary<Material, List<MaterialMapping>> mappings = new Dictionary<Material, List<MaterialMapping>>();
		//
		// // TODO: should we use a static switch instead?
		// public void RegisterMapping(Material mat, string propertyName, string exportedPropertyName)
		// {
		// 	if (!mappings.ContainsKey(mat))
		// 	{
		// 		mappings.Add(mat, new List<MaterialMapping>());
		// 	}
		// 	mappings[mat].Add(new MaterialMapping() { propertyName = propertyName, exportName = exportedPropertyName });
		// }

		public void Resolve(GLTFSceneExporter exporter)
		{
			foreach (var reg in registered)
			{
				animationPointerResolverMarker.Begin();
				switch (reg.animatedObject)
				{
					case Light light:
						reg.path = "/extensions/KHR_lights_punctual/lights/" + "0" + "/" + reg.propertyBinding;
						break;
					case Component comp:
						reg.path = "/nodes/" + exporter.GetAnimationTargetIdFromTransform(comp.transform) + "/" + reg.propertyBinding;
						var componentPath = reg.path;
						foreach (var res in exporter.pointerResolvers)
						{
							if (res.TryResolve(reg.animatedObject, ref componentPath))
								reg.path = componentPath;
						}
						break;
					case Material mat:
						reg.path = "/materials/" + exporter.GetAnimationTargetIdFromMaterial(mat) + "/" + reg.propertyBinding;
						break;
				}
				animationPointerResolverMarker.End();
			}
		}
	}
}
