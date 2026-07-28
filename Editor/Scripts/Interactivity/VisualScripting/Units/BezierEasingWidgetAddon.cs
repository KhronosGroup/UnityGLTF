using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// The shared header addon of the interpolate units: a preset picker plus a preview of the
    /// easing curve the node will actually evaluate at runtime.
    ///
    /// The control points stay ordinary Point A / Point B value inputs, so the preview reflects
    /// their port defaults and the picker writes back into them. When either port is connected the
    /// curve is decided at runtime and the addon goes read-only.
    /// </summary>
    public static class BezierEasingWidgetAddon
    {
        private static readonly GUIContent[] connectedContents = { new GUIContent("Driven by connection") };

        private static readonly GUIContent invalidTooltip = new GUIContent(string.Empty,
            "The x component of Point A and Point B must be within [0, 1]. " +
            "Outside that range KHR_interactivity requires the node to raise its err flow.");

        public static float GetWidth()
        {
            return BezierEasingGUI.PreferredWidth;
        }

        public static float GetHeight()
        {
            return BezierEasingGUI.GetHeight();
        }

        /// <summary>Draws the addon. Returns true when the preset picker changed the port defaults.</summary>
        public static bool Draw(Rect position, ValueInput point1Port, ValueInput point2Port)
        {
            if (point1Port == null || point2Port == null)
            {
                return false;
            }

            var connected = point1Port.hasValidConnection || point2Port.hasValidConnection;

            var point1 = GetDefault(point1Port);
            var point2 = GetDefault(point2Port);

            var popupPosition = new Rect(position.x, position.y, position.width, BezierEasingGUI.PopupHeight);
            var curvePosition = new Rect(
                position.x,
                popupPosition.yMax + BezierEasingGUI.SpaceBetween,
                position.width,
                BezierEasingGUI.CurveHeight);

            var changed = false;

            using (new EditorGUI.DisabledScope(connected))
            {
                if (connected)
                {
                    EditorGUI.Popup(popupPosition, 0, connectedContents);
                }
                else if (BezierEasingGUI.PresetPopup(popupPosition, ref point1, ref point2))
                {
                    var context = LudiqGraphsEditorUtility.editedContext.value;

                    context?.BeginEdit();
                    UndoUtility.RecordEditedObject("Set Easing Preset");

                    point1Port.SetDefaultValue(point1);
                    point2Port.SetDefaultValue(point2);

                    GUI.changed = true;
                    context?.EndEdit();

                    changed = true;
                }
            }

            BezierEasingGUI.CurveField(curvePosition, point1, point2, connected);

            if (!BezierEasingGUI.IsValid(point1, point2))
            {
                EditorGUI.LabelField(curvePosition, invalidTooltip);
            }

            return changed;
        }

        private static Vector2 GetDefault(ValueInput port)
        {
            if (port.unit.defaultValues.TryGetValue(port.key, out var value) && value is Vector2 vector)
            {
                return vector;
            }

            return Vector2.zero;
        }
    }
}
