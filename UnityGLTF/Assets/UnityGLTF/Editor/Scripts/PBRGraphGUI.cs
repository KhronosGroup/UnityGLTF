/*

// Can't use this on 2020 + 2021: ShaderGraphLitGui is private for whatever reasons.
// ShaderGraphLitGUI

using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
	public class PBRGraphGUI : ShaderGUI
	{
		private static readonly int ThicknessFactor = Shader.PropertyToID("thicknessFactor");
		private static readonly int TransmissionFactor = Shader.PropertyToID("transmissionFactor");
		private static readonly int IridescenceFactor = Shader.PropertyToID("iridescenceFactor");
		private static readonly int SpecularFactor = Shader.PropertyToID("specularFactor");

#if UNITY_2021_1_OR_NEWER
		public override void ValidateMaterial(Material material) => MaterialExtensions.ValidateMaterialKeywords(material);
#endif

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			EditorGUI.BeginChangeCheck();

			base.OnGUI(materialEditor, properties);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var t in materialEditor.targets)
				{
					if (t is Material material)
						MaterialExtensions.ValidateMaterialKeywords(material);
				}
			}
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			Debug.Log("New shader: " + newShader + ", old shader: " + oldShader);
			base.AssignNewShaderToMaterial(material, oldShader, newShader);
		}
	}
}

*/
