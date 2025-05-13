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
		Vector2 ThicknessTextureOffset { get; set; }
		double ThicknessTextureRotation { get; set; }
		Vector2 ThicknessTextureScale { get; set; }
		int ThicknessTextureTexCoord { get; set; }
		double AttenuationDistance { get; set; }
		Color AttenuationColor { get; set; }
	}

	public interface ITransmissionMap : IMetalRoughUniformMap
	{
		double TransmissionFactor { get; set; }
		Texture TransmissionTexture { get; set; }
		Vector2 TransmissionTextureOffset { get; set; }
		double TransmissionTextureRotation { get; set; }
		Vector2 TransmissionTextureScale { get; set; }
		int TransmissionTextureTexCoord { get; set; }
	}
	
	public interface ISheenMap : IMetalRoughUniformMap
	{
	    float SheenRoughnessFactor { get; set; }
	    
	    Color SheenColorFactor { get; set; }
	    
	    Texture SheenColorTexture { get; set; }

	    double SheenColorTextureRotation { get; set; }

	    Vector2 SheenColorTextureOffset { get; set; }

	    Vector2 SheenColorTextureScale { get; set; }

	    int SheenColorTextureTexCoord { get; set; }
	    
	    Texture SheenRoughnessTexture { get; set; }

	    double SheenRoughnessTextureRotation { get; set; }

	    Vector2 SheenRoughnessTextureOffset { get; set; }

	    Vector2 SheenRoughnessTextureScale { get; set; }

	    int SheenRoughnessTextureTexCoord { get; set; }

	}

	public interface IDispersionMap : ITransmissionMap
	{
		float Dispersion { get; set; }
	}

	public interface IIORMap : IMetalRoughUniformMap
	{
		double IOR { get; set; }
	}

	public interface ISpecularMap : IMetalRoughUniformMap
	{
		double SpecularFactor { get; set; }
		Texture SpecularTexture { get; set; }
		Vector2 SpecularTextureOffset { get; set; }
		double SpecularTextureRotation { get; set; }
		Vector2 SpecularTextureScale { get; set; }
		int SpecularTextureTexCoord { get; set; }
		Color SpecularColorFactor { get; set; }
		Texture SpecularColorTexture { get; set; }
		Vector2 SpecularColorTextureOffset { get; set; }
		double SpecularColorTextureRotation { get; set; }
		Vector2 SpecularColorTextureScale { get; set; }
		int SpecularColorTextureTexCoord { get; set; }
	}

	public interface IIridescenceMap : IMetalRoughUniformMap
	{
		double IridescenceFactor { get; set; }
		double IridescenceIor { get; set; }
		double IridescenceThicknessMinimum { get; set; }
		double IridescenceThicknessMaximum { get; set; }
		Texture IridescenceTexture { get; set; }
		Vector2 IridescenceTextureOffset { get; set; }
		double IridescenceTextureRotation { get; set; }
		Vector2 IridescenceTextureScale { get; set; }
		int IridescenceTextureTexCoord { get; set; }

		Texture IridescenceThicknessTexture { get; set; }
		Vector2 IridescenceThicknessTextureOffset { get; set; }
		double IridescenceThicknessTextureRotation { get; set; }
		Vector2 IridescenceThicknessTextureScale { get; set; }
		int IridescenceThicknessTextureTexCoord { get; set; }
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
		Vector2 ClearcoatTextureOffset { get; set; }
		double ClearcoatTextureRotation { get; set; }
		Vector2 ClearcoatTextureScale { get; set; }
		int ClearcoatTextureTexCoord { get; set; }

		double ClearcoatRoughnessFactor { get; set; }

		Texture ClearcoatRoughnessTexture { get; set; }
		Vector2 ClearcoatRoughnessTextureOffset { get; set; }
		double ClearcoatRoughnessTextureRotation { get; set; }
		Vector2 ClearcoatRoughnessTextureScale { get; set; }
		int ClearcoatRoughnessTextureTexCoord { get; set; }
	}

	public interface IClearcoatNormalMap
	{
		Texture ClearcoatNormalTexture { get; set; }
		Vector2 ClearcoatNormalTextureOffset { get; set; }
		double ClearcoatNormalTextureRotation { get; set; }
		Vector2 ClearcoatNormalTextureScale { get; set; }
		int ClearcoatNormalTextureTexCoord { get; set; }
	}
	
	public interface IAnisotropyMap
	{
		Texture anisotropyTexture { get; set; }
		Vector2 anisotropyTextureOffset { get; set; }
		double anisotropyTextureRotation { get; set; }
		Vector2 anisotropyTextureScale { get; set; }
		int anisotropyTextureTexCoord { get; set; }
		double anisotropyStrength { get; set; }
		double anisotropyRotation { get; set; }
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
