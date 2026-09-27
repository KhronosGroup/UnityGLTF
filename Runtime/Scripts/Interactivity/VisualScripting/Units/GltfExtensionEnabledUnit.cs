namespace Unity.VisualScripting
{
    /// <summary>
    /// Reads whether a given glTF extension is used by the asset and supported by the presenting
    /// implementation, exposed by KHR_interactivity as the read-only
    /// asset/extensions/&lt;EXTENSION_NAME&gt;/enabled pointer (spec §4.2.1 Asset Capabilities).
    /// </summary>
    [UnitCategory("glTF/Interactivity")]
    [UnitTitle("Is glTF Extension Enabled")]
    public class GltfExtensionEnabledUnit : Unit
    {
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Extension")]
        public string extensionName
        {
            get => _extensionName;
            set => _extensionName = value;
        }

        [SerializeAs(nameof(extensionName))]
        private string _extensionName = "KHR_node_visibility";

        [DoNotSerialize]
        [PortLabelHidden]
        public ValueOutput enabled { get; private set; }

        protected override void Definition()
        {
            // Best-effort constant for in-editor play mode; the real value is provided by the
            // KHR_interactivity runtime of the viewer that presents the exported asset.
            enabled = ValueOutput<bool>("enabled", _ => false);
        }
    }
}
