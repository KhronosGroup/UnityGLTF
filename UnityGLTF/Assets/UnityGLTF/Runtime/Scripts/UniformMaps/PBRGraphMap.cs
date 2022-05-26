using GLTF.Schema;
using UnityEngine;
using UnityGLTF;

public class PBRGraphMap : IMetalRoughUniformMap, IVolumeMap, ITransmissionMap, IIORMap
{
	protected Material _material;

	public PBRGraphMap(int MaxLOD = 1000) : this("UnityGltf/PBR", MaxLOD) {}

	protected PBRGraphMap(string shaderName, int MaxLOD = 1000)
	{
		var s = Shader.Find(shaderName);
		if (s == null)
		{
			throw new ShaderNotFoundException(shaderName + " not found. Did you forget to add it to the build?");
		}
		_material = new Material(s);
	}

	protected PBRGraphMap(Material mat)
	{
		_material = mat;
	}

	public Material Material => _material;

	public Texture NormalTexture
	{
		get => _material.GetTexture("_NormalTexture");
		set => _material.SetTexture("_NormalTexture", value);
	}

	public int NormalTexCoord
	{
		get => 0;
		set {}
	}

	public double NormalTexScale
	{
		get => _material.GetFloat("_NormalScale");
		set => _material.SetFloat("_NormalScale", (float) value);
	}

    public Vector2 NormalXOffset
    {
	    get => _material.GetTextureOffset("_NormalTexture");
	    set => _material.SetTextureOffset("_NormalTexture", value);
    }

    public double NormalXRotation { get; set; }

    public Vector2 NormalXScale
    {
	    get => _material.GetTextureScale("_NormalTexture");
	    set => _material.SetTextureScale("_NormalTexture", value);
    }

    public int NormalXTexCoord
    {
	    get => 0;
	    set {}
    }

    public Texture OcclusionTexture
    {
	    get => _material.GetTexture("_OcclusionTexture");
	    set => _material.SetTexture("_OcclusionTexture", value);
    }
    public int OcclusionTexCoord
    {
	    get => 0;
	    set {}
    }
    public double OcclusionTexStrength
    {
	    get => _material.GetFloat("_OcclusionStrength");
	    set => _material.SetFloat("_OcclusionStrength", (float) value);
    }
    public Vector2 OcclusionXOffset
    {
	    get => _material.GetTextureOffset("_OcclusionTexture");
	    set => _material.SetTextureOffset("_OcclusionTexture", value);
    }
    public double OcclusionXRotation { get; set; }
    public Vector2 OcclusionXScale
    {
	    get => _material.GetTextureScale("_OcclusionTexture");
	    set => _material.SetTextureScale("_OcclusionTexture", value);
    }
    public int OcclusionXTexCoord
    {
	    get => 0;
	    set {}
    }
    public Texture EmissiveTexture
    {
	    get => _material.GetTexture("_EmissiveTexture");
	    set => _material.SetTexture("_EmissiveTexture", value);
    }
    public int EmissiveTexCoord
    {
	    get => 0;
	    set {}
    }
    public Color EmissiveFactor
    {
	    get => _material.GetColor("_EmissiveFactor");
	    set => _material.SetColor("_EmissiveFactor", value);
    }
    public Vector2 EmissiveXOffset
    {
	    get => _material.GetTextureOffset("_EmissiveTexture");
	    set => _material.SetTextureOffset("_EmissiveTexture", value);
    }
    public double EmissiveXRotation { get; set; }
    public Vector2 EmissiveXScale
    {
	    get => _material.GetTextureScale("_EmissiveTexture");
	    set => _material.SetTextureScale("_EmissiveTexture", value);
    }
    public int EmissiveXTexCoord
    {
	    get => 0;
	    set {}
    }
    public AlphaMode AlphaMode { get; set; }
    public double AlphaCutoff
    {
	    get => _material.GetFloat("_AlphaCutoff");
	    set => _material.SetFloat("_AlphaCutoff", (float) value);
    }
    public bool DoubleSided { get; set; }
    public bool VertexColorsEnabled { get; set; }

    public IUniformMap Clone()
    {
	    return new PBRGraphMap(new Material(_material));
    }

    public Texture BaseColorTexture
    {
	    get => _material.GetTexture("_BaseColorTexture");
	    set => _material.SetTexture("_BaseColorTexture", value);
    }
    public int BaseColorTexCoord
    {
	    get => 0;
	    set {}
    }
    public Vector2 BaseColorXOffset
    {
	    get => _material.GetTextureOffset("_BaseColorTexture");
	    set => _material.SetTextureOffset("_BaseColorTexture", value);
    }
    public double BaseColorXRotation { get; set; }
    public Vector2 BaseColorXScale
    {
	    get => _material.GetTextureScale("_BaseColorTexture");
	    set => _material.SetTextureScale("_BaseColorTexture", value);
    }
    public int BaseColorXTexCoord
    {
	    get => 0;
	    set {}
    }
    public Color BaseColorFactor
    {
	    get => _material.GetColor("_BaseColorFactor");
	    set => _material.SetColor("_BaseColorFactor", value);
    }
    public Texture MetallicRoughnessTexture
    {
	    get => _material.GetTexture("_MetallicRoughnessTexture");
	    set => _material.SetTexture("_MetallicRoughnessTexture", value);
    }
    public int MetallicRoughnessTexCoord
    {
	    get => 0;
	    set {}
    }
    public Vector2 MetallicRoughnessXOffset
    {
	    get => _material.GetTextureOffset("_MetallicRoughnessTexture");
	    set => _material.SetTextureOffset("_MetallicRoughnessTexture", value);
    }
    public double MetallicRoughnessXRotation { get; set; }
    public Vector2 MetallicRoughnessXScale
    {
	    get => _material.GetTextureOffset("_MetallicRoughnessTexture");
	    set => _material.SetTextureOffset("_MetallicRoughnessTexture", value);
    }
    public int MetallicRoughnessXTexCoord
    {
	    get => 0;
	    set {}
    }
    public double MetallicFactor
    {
	    get => _material.GetFloat("_MetallicFactor");
	    set => _material.SetFloat("_MetallicFactor", (float) value);
    }
    public double RoughnessFactor
    {
	    get => _material.GetFloat("_RoughnessFactor");
	    set => _material.SetFloat("_RoughnessFactor", (float) value);
    }

    public double ThicknessFactor
    {
	    get => _material.GetFloat("_RoughnessFactor");
	    set => _material.SetFloat("_RoughnessFactor", (float) value);
    }
    public Texture ThicknessTexture
    {
	    get => _material.GetTexture("_ThicknessTexture");
	    set => _material.SetTexture("_ThicknessTexture", value);
    }
    public double AttenuationDistance
    {
	    get => _material.GetFloat("_RoughnessFactor");
	    set => _material.SetFloat("_RoughnessFactor", (float) value);
    }
    public Color AttenuationColor
    {
	    get => _material.GetColor("_AttenuationColor");
	    set => _material.SetColor("_AttenuationColor", value);
    }
    public double TransmissionFactor
    {
	    get => _material.GetFloat("_TransmissionFactor");
	    set => _material.SetFloat("_TransmissionFactor", (float) value);
    }
    public Texture TransmissionTexture
    {
	    get => _material.GetTexture("_TransmissionTexture");
	    set => _material.SetTexture("_TransmissionTexture", value);
    }
    public double IOR
    {
	    get => _material.GetFloat("_IOR");
	    set => _material.SetFloat("_IOR", (float) value);
    }
}
