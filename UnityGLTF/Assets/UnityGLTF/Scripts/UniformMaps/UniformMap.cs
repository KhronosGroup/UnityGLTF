using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityGLTF
{
	public interface IUniformMap
	{
		Material Material { get; }

		Texture NormalTexture { get; set; }
		int NormalTexCoord { get; set; }
		double NormalTexScale { get; set; }
		Vector2 NormalXOffset { get; set; }
		double NormalXRotation { get; set; }
		Vector2 NormalXScale { get; set; }
		int NormalXTexCoord { get; set; }

		Texture OcclusionTexture { get; set; }
		int OcclusionTexCoord { get; set; }
		double OcclusionTexStrength { get; set; }
		Vector2 OcclusionXOffset { get; set; }
		double OcclusionXRotation { get; set; }
		Vector2 OcclusionXScale { get; set; }
		int OcclusionXTexCoord { get; set; }

		Texture EmissiveTexture { get; set; }
		int EmissiveTexCoord { get; set; }
		Color EmissiveFactor { get; set; }
		Vector2 EmissiveXOffset { get; set; }
		double EmissiveXRotation { get; set; }
		Vector2 EmissiveXScale { get; set; }
		int EmissiveXTexCoord { get; set; }

		GLTF.Schema.AlphaMode AlphaMode { get; set; }
		double AlphaCutoff { get; set; }
		bool DoubleSided { get; set; }
		bool VertexColorsEnabled { get; set; }

		IUniformMap Clone();
	}

	public interface IMetalRoughUniformMap : IUniformMap
	{
		Texture BaseColorTexture { get; set; }
		int BaseColorTexCoord { get; set; }
		Vector2 BaseColorXOffset { get; set; }
		double BaseColorXRotation { get; set; }
		Vector2 BaseColorXScale { get; set; }
		int BaseColorXTexCoord { get; set; }

		Color BaseColorFactor { get; set; }

		Texture MetallicRoughnessTexture { get; set; }
		int MetallicRoughnessTexCoord { get; set; }
		Vector2 MetallicRoughnessXOffset { get; set; }
		double MetallicRoughnessXRotation { get; set; }
		Vector2 MetallicRoughnessXScale { get; set; }
		int MetallicRoughnessXTexCoord { get; set; }

		double MetallicFactor { get; set; }
		double RoughnessFactor { get; set; }
	}

	public interface ISpecGlossUniformMap : IUniformMap
	{
		Texture DiffuseTexture { get; set; }
		int DiffuseTexCoord { get; set; }
		Vector2 DiffuseXOffset { get; set; }
		double DiffuseXRotation { get; set; }
		Vector2 DiffuseXScale { get; set; }
		int DiffuseXTexCoord { get; set; }

		Color DiffuseFactor { get; set; }

		Texture SpecularGlossinessTexture { get; set; }
		int SpecularGlossinessTexCoord { get; set; }
		Vector2 SpecularGlossinessXOffset { get; set; }
		double SpecularGlossinessXRotation { get; set; }
		Vector2 SpecularGlossinessXScale { get; set; }
		int SpecularGlossinessXTexCoord { get; set; }

		Vector3 SpecularFactor { get; set; }
		double GlossinessFactor { get; set; }
	}

	public interface IUnlitUniformMap : IUniformMap
	{
		Texture BaseColorTexture { get; set; }
		int BaseColorTexCoord { get; set; }
		Vector2 BaseColorXOffset { get; set; }
		double BaseColorXRotation { get; set; }
		Vector2 BaseColorXScale { get; set; }
		int BaseColorXTexCoord { get; set; }

		Color BaseColorFactor { get; set; }
	}
}
