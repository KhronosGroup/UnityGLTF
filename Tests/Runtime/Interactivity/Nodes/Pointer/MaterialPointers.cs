namespace UnityGLTF.Interactivity.Playback.Tests
{
    partial class PointerNodesTests
    {

        private static readonly (string pointer, string type)[] MATERIAL_POINTERS = new (string, string)[]
        {
            ("/materials/{nodeIndex}/alphaCutoff", "float"),
            ("/materials/{nodeIndex}/emissiveFactor", "float3"),
            ("/materials/{nodeIndex}/normalTexture/scale", "float"),
            ("/materials/{nodeIndex}/occlusionTexture/strength", "float"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/baseColorFactor", "float4"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/metallicFactor", "float"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/roughnessFactor", "float"),

            ("/materials/{nodeIndex}/normalTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/normalTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/normalTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/occlusionTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/occlusionTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/occlusionTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/emissiveTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/emissiveTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/emissiveTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/pbrMetallicRoughness/metallicRoughnessTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/metallicRoughnessTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/pbrMetallicRoughness/metallicRoughnessTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatFactor", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatRoughnessFactor", "float"),
            //("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatNormalTexture/scale", "float"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatRoughnessTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatRoughnessTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatRoughnessTexture/extensions/KHR_texture_transform/scale", "float2"),

            //("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatNormalTexture/extensions/KHR_texture_transform/offset", "float2"),
            //("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatNormalTexture/extensions/KHR_texture_transform/rotation", "float"),
            //("/materials/{nodeIndex}/extensions/KHR_materials_clearcoat/clearcoatNormalTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_dispersion/dispersion", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_ior/ior", "float"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceFactor", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceIor", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceThicknessMinimum", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceThicknessMaximum", "float"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceThicknessTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceThicknessTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_iridescence/iridescenceThicknessTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenColorFactor", "float3"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenRoughnessFactor", "float"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenColorTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenColorTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenColorTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenRoughnessTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenRoughnessTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_sheen/sheenRoughnessTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularFactor", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularColorFactor", "float3"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularColorTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularColorTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_specular/specularColorTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_transmission/transmissionFactor", "float"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_transmission/transmissionTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_transmission/transmissionTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_transmission/transmissionTexture/extensions/KHR_texture_transform/scale", "float2"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/thicknessFactor", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/attenuationDistance", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/attenuationColor", "float3"),

            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/thicknessTexture/extensions/KHR_texture_transform/offset", "float2"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/thicknessTexture/extensions/KHR_texture_transform/rotation", "float"),
            ("/materials/{nodeIndex}/extensions/KHR_materials_volume/thicknessTexture/extensions/KHR_texture_transform/scale", "float2"),
       };
    }
}