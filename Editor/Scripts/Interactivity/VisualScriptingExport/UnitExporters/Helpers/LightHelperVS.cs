using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using LightType = UnityEngine.LightType;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Pointers to KHR_lights_punctual lights. The values are converted like GLTFSceneExporter.ExportLight does it:
    /// linear color, intensity * PI (squared without lightsUseLinearIntensity) and the spot cone half angle in radians.
    /// </summary>
    public static class LightHelperVS
    {
        /// <summary> ExportLight sets the inner cone angle to 80% of the outer cone angle. </summary>
        public const float InnerConeAngleFactor = 0.8f;

        /// <summary>
        /// The Light referenced by target when it's known at export time (literal, default value or self), otherwise null.
        /// </summary>
        public static bool TryGetLiteralLight(UnitExporter unitExporter, ValueInput target, out Light light)
        {
            light = null;
            if (!unitExporter.IsInputLiteralOrDefaultValue(target, out var value))
                return false;

            switch (value)
            {
                case Light l:
                    light = l;
                    break;
                case GameObject go:
                    light = go.GetComponent<Light>();
                    break;
                case Component component:
                    light = component.GetComponent<Light>();
                    break;
            }
            return true;
        }

        /// <summary>
        /// Configures a pointer node for a light property (e.g. "color" or "spot/outerConeAngle").
        /// A light known at export time is referenced directly, otherwise the light ref is read from
        /// the node pointer /nodes/{}/extensions/KHR_lights_punctual/light.
        /// </summary>
        public static bool SetupLightPointer(UnitExporter unitExporter, GltfInteractivityExportNode pointerNode, ValueInput target, string property, int gltfType)
        {
            PointersHelper.SetupPointerTemplateAndTargetInput(pointerNode, PointersHelper.IdPointerLightRef,
                PointersHelper.IdPointerTemplLightByRef + "/" + property, gltfType);

            if (TryGetLiteralLight(unitExporter, target, out var light))
            {
                if (light == null)
                {
                    UnitExportLogging.AddErrorLog(unitExporter.unit, "Target has no Light component.");
                    return false;
                }

                var lightIndex = unitExporter.vsExportContext.exporter.GetLightIndex(light);
                if (lightIndex == -1)
                {
                    UnitExportLogging.AddErrorLog(unitExporter.unit, $"Light {light.name} is not exported. The KHR_lights_punctual export plugin and the Light component must be enabled, and only directional, point and spot lights are supported.");
                    return false;
                }

                pointerNode.ValueIn(PointersHelper.IdPointerLightRef)
                    .SetValue(new StaticRefPointer($"/extensions/KHR_lights_punctual/lights/{lightIndex}"))
                    .SetType(TypeRestriction.LimitToRef);
                return true;
            }

            var getLight = unitExporter.CreateNode<Pointer_GetNode>();
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getLight, PointersHelper.IdPointerNodeRef, target,
                PointersHelper.IdPointerNodeLightByRef, GltfTypes.Ref);
            pointerNode.ValueIn(PointersHelper.IdPointerLightRef)
                .ConnectToSource(getLight.FirstValueOut().ExpectedType(ExpectedType.Ref))
                .SetType(TypeRestriction.LimitToRef);
            return true;
        }

        private static readonly float SpotSquaredIntensityFactor = Mathf.PI * Mathf.PI / 4f * Mathf.Sqrt(2f);

        /// <summary>
        /// glTF intensity = Unity intensity * factor, or Unity intensity² * factor when squared (see ExportLight).
        /// Without lightsUseLinearIntensity spot lights use another factor. When the light is only known at runtime,
        /// the factor is selected by the light having the spot pointers, which only exist for spot lights.
        /// </summary>
        private static MathOperand IntensityFactor(UnitExporter unitExporter, ValueInput target, out bool squared)
        {
            squared = !GraphicsSettings.lightsUseLinearIntensity;
            if (!squared)
                return Mathf.PI;

            if (TryGetLiteralLight(unitExporter, target, out var light) && light != null)
                return light.type == LightType.Spot ? SpotSquaredIntensityFactor : Mathf.PI;

            var getSpot = unitExporter.CreateNode<Pointer_GetNode>();
            SetupLightPointer(unitExporter, getSpot, target, "spot/outerConeAngle", GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float));
            var isSpot = getSpot.ValueOut(Pointer_GetNode.IdIsValid).ExpectedType(ExpectedType.Bool);
            return MathExpression.Float(unitExporter).Select(isSpot, SpotSquaredIntensityFactor, Mathf.PI);
        }

        public static ValueOutRef IntensityToGltf(UnitExporter unitExporter, ValueInput target, MathOperand unityIntensity)
        {
            var m = MathExpression.Float(unitExporter);
            var factor = IntensityFactor(unitExporter, target, out var squared);
            return squared ? m.Mul(m.Mul(unityIntensity, unityIntensity), factor) : m.Mul(unityIntensity, factor);
        }

        public static ValueOutRef IntensityFromGltf(UnitExporter unitExporter, ValueInput target, MathOperand gltfIntensity)
        {
            var m = MathExpression.Float(unitExporter);
            var factor = IntensityFactor(unitExporter, target, out var squared);
            return squared ? m.Sqrt(m.Div(gltfIntensity, factor)) : m.Div(gltfIntensity, factor);
        }

        /// <summary> Unity Color (gamma, float4) to the linear glTF light color (float3). </summary>
        public static ValueOutRef ColorToGltf(UnitExporter unitExporter, MathOperand unityColor)
        {
            ColorFunctions.Extract(unitExporter, unityColor, out var r, out var g, out var b, out _);
            var combine = unitExporter.CreateNode<Math_Combine3Node>();
            combine.ValueIn(Math_Combine3Node.IdValueA).ConnectToSource(ColorFunctions.GammaToLinear(unitExporter, r)).SetType(TypeRestriction.LimitToFloat);
            combine.ValueIn(Math_Combine3Node.IdValueB).ConnectToSource(ColorFunctions.GammaToLinear(unitExporter, g)).SetType(TypeRestriction.LimitToFloat);
            combine.ValueIn(Math_Combine3Node.IdValueC).ConnectToSource(ColorFunctions.GammaToLinear(unitExporter, b)).SetType(TypeRestriction.LimitToFloat);
            return combine.FirstValueOut().ExpectedType(ExpectedType.Float3);
        }

        /// <summary> Linear glTF light color (float3) to a Unity Color (gamma, float4) with alpha 1. </summary>
        public static ValueOutRef ColorFromGltf(UnitExporter unitExporter, ValueOutRef gltfColor)
        {
            var extract = unitExporter.CreateNode<Math_Extract3Node>();
            extract.ValueIn(Math_Extract3Node.IdValueIn).ConnectToSource(gltfColor).SetType(TypeRestriction.LimitToFloat3);
            ValueOutRef Channel(string socket) => ColorFunctions.LinearToGamma(unitExporter, extract.ValueOut(socket).ExpectedType(ExpectedType.Float));
            return ColorFunctions.Combine(unitExporter, Channel(Math_Extract3Node.IdValueOutX), Channel(Math_Extract3Node.IdValueOutY), Channel(Math_Extract3Node.IdValueOutZ), 1f);
        }
    }
}
