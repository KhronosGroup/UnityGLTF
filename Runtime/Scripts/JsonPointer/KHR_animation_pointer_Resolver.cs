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

		public void Resolve(GLTFSceneExporter exporter)
		{
			foreach (var reg in registered)
			{
				animationPointerResolverMarker.Begin();
				int id = exporter.GetIndex(reg.animatedObject);
				switch (reg.animatedObject)
				{
					case Light light:
						reg.path = "/extensions/KHR_lights_punctual/lights/" + id + "/" + reg.propertyBinding;
						break;
					case Camera camera:
						if (reg.propertyBinding == "backgroundColor")
						{
							id = exporter.GetTransformIndex(camera.transform);
							goto ResolveComponent;
						}
						reg.path = "/cameras/" + id + "/" + reg.propertyBinding;
						break;
					case SkinnedMeshRenderer smr:
						if (smr && reg.propertyBinding == "weights")
						{
							reg.path = "/nodes/" + id + "/" + reg.propertyBinding;
							break;
						}
						goto ResolveComponent;
					case Component comp:
					case GameObject g:
					ResolveComponent:
						reg.path = "/nodes/" + id + "/" + reg.propertyBinding;
						var componentPath = reg.path;

						var anyOtherResolverWasAbleToResolve = false;
						foreach (var res in exporter.pointerResolvers)
						{
							if (res.TryResolve(reg.animatedObject, ref componentPath))
							{
								reg.path = componentPath;
								anyOtherResolverWasAbleToResolve = true;
								break;
							}
						}
						if (exporter.pointerResolvers.Count > 0 && !anyOtherResolverWasAbleToResolve)
						{
							// we don't need to warn for regular transforms that are not RectTransforms,
							// but we want to warn for everything else that may be animated.
							if (!(reg.animatedObject is Transform && !(reg.animatedObject is RectTransform)))
								Debug.LogWarning("Wasn't able to resolve animation pointer for " + reg.animatedObject + " at " + componentPath + ". You can attach custom resolvers to animate properties in extensions.", reg.animatedObject as Object);
						}
						else if (!anyOtherResolverWasAbleToResolve)
						{
							// we don't need to warn for regular transforms that are not RectTransforms,
							// but we want to warn for everything else that may be animated.
							if (!(reg.animatedObject is Transform && !(reg.animatedObject is RectTransform)))
								Debug.LogWarning("Wasn't able to resolve animation pointer for " + reg.animatedObject + " at " + componentPath + ". You can attach custom resolvers to animate properties in extensions.", reg.animatedObject as Object);
						}
						break;
					case Material mat:
						reg.path = "/materials/" + id + "/" + reg.propertyBinding;
						break;
				}
				animationPointerResolverMarker.End();
			}
		}
	}
}
