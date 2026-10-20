using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using LightType = UnityEngine.LightType;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Light.color, intensity, range and spotAngle as KHR_lights_punctual pointers (see <see cref="LightHelperVS"/>).
    /// Setting spotAngle also sets the inner cone angle, which ExportLight derives from it.
    /// </summary>
    public abstract class LightMemberUnitExport : IUnitExporter
    {
        protected static readonly string[] Members =
        {
            nameof(Light.color), nameof(Light.intensity), nameof(Light.range), nameof(Light.spotAngle),
        };

        protected readonly string member;

        public abstract Type unitType { get; }

        protected LightMemberUnitExport(string member)
        {
            this.member = member;
        }

        public abstract bool InitializeInteractivityNodes(UnitExporter unitExporter);

        protected static string GltfProperty(string member, out int gltfType)
        {
            gltfType = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float);
            switch (member)
            {
                case nameof(Light.color):
                    gltfType = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float3);
                    return "color";
                case nameof(Light.intensity):
                    return "intensity";
                case nameof(Light.range):
                    return "range";
                case nameof(Light.spotAngle):
                    return "spot/outerConeAngle";
                default:
                    return null;
            }
        }

        /// <summary> Spot and range pointers only exist for some light types, check it when the light is known. </summary>
        protected bool IsSupportedByLightType(UnitExporter unitExporter, ValueInput target)
        {
            if (!LightHelperVS.TryGetLiteralLight(unitExporter, target, out var light) || light == null)
                return true;

            if (member == nameof(Light.spotAngle) && light.type != LightType.Spot)
            {
                UnitExportLogging.AddErrorLog(unitExporter.unit, $"Light.spotAngle is only supported on spot lights ({light.name} is a {light.type} light).");
                return false;
            }
            if (member == nameof(Light.range) && light.type == LightType.Directional)
            {
                UnitExportLogging.AddErrorLog(unitExporter.unit, $"Light.range is not supported on directional lights ({light.name}).");
                return false;
            }
            return true;
        }
    }

    public class Light_GetMemberUnitExport : LightMemberUnitExport
    {
        public override Type unitType => typeof(GetMember);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            foreach (var member in Members)
                GetMemberUnitExport.RegisterMemberExporter(typeof(Light), member, new Light_GetMemberUnitExport(member));
        }

        private Light_GetMemberUnitExport(string member) : base(member)
        {
        }

        public override bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            if (!IsSupportedByLightType(unitExporter, unit.target))
                return false;

            var property = GltfProperty(member, out var gltfType);
            var getNode = unitExporter.CreateNode<Pointer_GetNode>();
            if (!LightHelperVS.SetupLightPointer(unitExporter, getNode, unit.target, property, gltfType))
                return false;
            var value = getNode.FirstValueOut().ExpectedType(ExpectedType.GtlfType(gltfType));

            var m = MathExpression.Float(unitExporter);
            ValueOutRef result;
            switch (member)
            {
                case nameof(Light.color):
                    result = LightHelperVS.ColorFromGltf(unitExporter, value);
                    break;
                case nameof(Light.intensity):
                    result = LightHelperVS.IntensityFromGltf(unitExporter, unit.target, value);
                    break;
                case nameof(Light.spotAngle):
                    // Unity: full cone angle in degrees
                    result = m.Mul(value, 2f * Mathf.Rad2Deg);
                    break;
                default:
                    result = value;
                    break;
            }

            result.MapToPort(unit.value);
            return true;
        }
    }

    public class Light_SetMemberUnitExport : LightMemberUnitExport
    {
        public override Type unitType => typeof(SetMember);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            foreach (var member in Members)
                SetMemberUnitExport.RegisterMemberExporter(typeof(Light), member, new Light_SetMemberUnitExport(member));
        }

        private Light_SetMemberUnitExport(string member) : base(member)
        {
        }

        public override bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetMember;
            if (!IsSupportedByLightType(unitExporter, unit.target))
                return false;

            var property = GltfProperty(member, out var gltfType);
            var setNode = unitExporter.CreateNode<Pointer_SetNode>();
            if (!LightHelperVS.SetupLightPointer(unitExporter, setNode, unit.target, property, gltfType))
                return false;

            var m = MathExpression.Float(unitExporter);
            var lastSetNode = setNode;
            switch (member)
            {
                case nameof(Light.color):
                    setNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(LightHelperVS.ColorToGltf(unitExporter, unit.input));
                    break;
                case nameof(Light.intensity):
                    setNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(LightHelperVS.IntensityToGltf(unitExporter, unit.target, unit.input));
                    break;
                case nameof(Light.spotAngle):
                    var outerConeAngle = m.Mul(unit.input, Mathf.Deg2Rad / 2f);
                    setNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(outerConeAngle);

                    lastSetNode = unitExporter.CreateNode<Pointer_SetNode>();
                    if (!LightHelperVS.SetupLightPointer(unitExporter, lastSetNode, unit.target, "spot/innerConeAngle", gltfType))
                        return false;
                    lastSetNode.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(m.Mul(outerConeAngle, LightHelperVS.InnerConeAngleFactor));
                    setNode.FlowOut(Pointer_SetNode.IdFlowOut).ConnectToFlowDestination(lastSetNode.FlowIn(Pointer_SetNode.IdFlowIn));
                    break;
                default:
                    setNode.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.input).SetType(TypeRestriction.LimitToFloat);
                    break;
            }

            setNode.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.assign);
            lastSetNode.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.assigned);
            unitExporter.ByPassValue(unit.input, unit.output);
            return true;
        }
    }
}
