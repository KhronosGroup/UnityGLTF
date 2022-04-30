using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Extensions
{
	public class KHR_animation_pointer_Resolver
	{
		private readonly List<KHR_animation_pointer> registered = new List<KHR_animation_pointer>();

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
				switch (reg.animatedObject)
				{
					case Component comp:
						// TODO: how to get component id
						reg.path = "/nodes/" + exporter.GetAnimationTargetIdFromTransform(comp.transform) + "/" + reg.propertyBinding;
						break;
					case Material mat:
						var path = ResolvePropertyBindingName(reg.propertyBinding);
						// reg.channel.Path = path;
						reg.path = "/materials/" + exporter.GetAnimationTargetIdFromMaterial(mat) + "/" + path;
						break;
				}
			}
		}

		private string ResolvePropertyBindingName(string name)
		{
			switch (name)
			{
				case "_BaseColor":
					return "baseColorFactor";
				// TODO smoothness values need to be inverted! different shading model!
				case "_Smoothness":
					return "roughnessFactor";
				case "_Metallic":
					return "metallicFactor";
			}
			return name;
		}
	}
}
