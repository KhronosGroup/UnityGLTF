using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Projection properties of Camera.main, read from the read-only activeCamera pointers. Only supported on
    /// Camera.main: the active camera of the viewer, which is not necessarily a camera of the glTF.
    /// The perspective pointers are NaN for an orthographic camera and vice-versa, so the values that exist for
    /// both projections (aspect, near, far) fall back to the orthographic pointers.
    /// </summary>
    public class CameraMainProjectionUnitExport : IUnitExporter
    {
        public Type unitType => typeof(GetMemberUnitExport);

        private const string Prefix = "/extensions/KHR_interactivity/activeCamera/";

        private readonly string member;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            foreach (var member in new[]
                     {
                         nameof(Camera.fieldOfView), nameof(Camera.aspect), nameof(Camera.nearClipPlane),
                         nameof(Camera.farClipPlane), nameof(Camera.orthographic), nameof(Camera.orthographicSize),
                     })
                GetMemberUnitExport.RegisterMemberExporter(typeof(Camera), member, new CameraMainProjectionUnitExport(member));
        }

        private CameraMainProjectionUnitExport(string member)
        {
            this.member = member;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            if (!UnitsHelper.IsMainCameraInInput(unit.target))
            {
                Debug.LogWarning($"Camera.{member} is only supported on Camera.main (activeCamera pointers).");
                return false;
            }

            ValueOutRef Get(string pointer)
            {
                var getNode = unitExporter.CreateNode<Pointer_GetNode>();
                PointersHelper.AddPointerConfig(getNode, Prefix + pointer, GltfTypes.Float);
                return getNode.FirstValueOut().ExpectedType(ExpectedType.Float);
            }

            ValueOutRef IsNaN(ValueOutRef value)
            {
                var isNaN = unitExporter.CreateNode<Math_IsNaNNode>();
                isNaN.ValueIn(Math_IsNaNNode.IdValueA).ConnectToSource(value);
                return isNaN.FirstValueOut().ExpectedType(ExpectedType.Bool);
            }

            // perspective value, or the orthographic one if the perspective value is NaN
            ValueOutRef PerspectiveOrOrthographic(ValueOutRef perspective, ValueOutRef orthographic)
            {
                var select = unitExporter.CreateNode<Math_SelectNode>();
                select.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(IsNaN(perspective));
                select.ValueIn(Math_SelectNode.IdValueA).ConnectToSource(orthographic);
                select.ValueIn(Math_SelectNode.IdValueB).ConnectToSource(perspective);
                return select.FirstValueOut().ExpectedType(ExpectedType.Float);
            }

            ValueOutRef result;
            switch (member)
            {
                case nameof(Camera.fieldOfView):
                    // Unity: vertical field of view in degrees
                    var deg = unitExporter.CreateNode<Math_DegNode>();
                    deg.ValueIn(Math_DegNode.IdInputA).ConnectToSource(Get("perspective/yfov"));
                    result = deg.FirstValueOut().ExpectedType(ExpectedType.Float);
                    break;
                case nameof(Camera.aspect):
                    var div = unitExporter.CreateNode<Math_DivNode>();
                    div.ValueIn(Math_DivNode.IdValueA).ConnectToSource(Get("orthographic/xmag"));
                    div.ValueIn(Math_DivNode.IdValueB).ConnectToSource(Get("orthographic/ymag"));
                    result = PerspectiveOrOrthographic(Get("perspective/aspectRatio"), div.FirstValueOut().ExpectedType(ExpectedType.Float));
                    break;
                case nameof(Camera.nearClipPlane):
                    result = PerspectiveOrOrthographic(Get("perspective/znear"), Get("orthographic/znear"));
                    break;
                case nameof(Camera.farClipPlane):
                    result = PerspectiveOrOrthographic(Get("perspective/zfar"), Get("orthographic/zfar"));
                    break;
                case nameof(Camera.orthographic):
                    // NaN without an active camera, so orthographic is false then
                    var not = unitExporter.CreateNode<Math_NotNode>();
                    not.ValueIn(Math_NotNode.IdValueA).ConnectToSource(IsNaN(Get("orthographic/xmag")));
                    result = not.FirstValueOut().ExpectedType(ExpectedType.Bool);
                    break;
                case nameof(Camera.orthographicSize):
                    // Unity: half the vertical size
                    result = Get("orthographic/ymag");
                    break;
                default:
                    return false;
            }

            result.MapToPort(unit.value);
            return true;
        }
    }
}
