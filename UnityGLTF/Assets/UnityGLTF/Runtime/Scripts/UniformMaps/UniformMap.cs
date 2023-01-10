using UnityEngine;

namespace UnityGLTF
{
	public interface IUniformMap
	{
		Material Material { get; }

		GLTF.Schema.AlphaMode AlphaMode { get; set; }
		double AlphaCutoff { get; set; }
		bool DoubleSided { get; set; }
		bool VertexColorsEnabled { get; set; }

		IUniformMap Clone();
	}

	public interface ILitMap : IUniformMap
	{
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
	}

	public interface IMetalRoughUniformMap : ILitMap
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

	public interface IVolumeMap : IMetalRoughUniformMap
	{
		double ThicknessFactor { get; set; }
		Texture ThicknessTexture { get; set; }
		double AttenuationDistance { get; set; }
		Color AttenuationColor { get; set; }
	}

	public interface ITransmissionMap : IMetalRoughUniformMap
	{
		double TransmissionFactor { get; set; }
		Texture TransmissionTexture { get; set; }
	}

	public interface IIORMap : IMetalRoughUniformMap
	{
		double IOR { get; set; }
	}

	public interface ISpecularMap : IMetalRoughUniformMap
	{
		double SpecularFactor { get; set; }
		Texture SpecularTexture { get; set; }
		Color SpecularColorFactor { get; set; }
		Texture SpecularColorTexture { get; set; }
	}

	public interface IIridescenceMap : IMetalRoughUniformMap
	{
		double IridescenceFactor { get; set; }
		double IridescenceIor { get; set; }
		double IridescenceThicknessMinimum { get; set; }
		double IridescenceThicknessMaximum { get; set; }
		Texture IridescenceTexture { get; set; }
		Texture IridescenceThicknessTexture { get; set; }
	}

	public interface ISpecGlossUniformMap : ILitMap
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

	public interface IClearcoatMap : IMetalRoughUniformMap
	{
		double ClearcoatFactor { get; set; }
		Texture ClearcoatTexture { get; set; }
		double ClearcoatRoughnessFactor { get; set; }
		Texture ClearcoatRoughnessTexture { get; set; }
		Texture ClearcoatNormalTexture { get; set; }
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
