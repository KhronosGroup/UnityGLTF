namespace Unity.VisualScripting
{
    /// <summary>
    /// Reads the glTF spec version used to present the asset, exposed by KHR_interactivity as the
    /// read-only asset/majorVersion and asset/minorVersion pointers (spec §4.2.1 Asset Capabilities).
    /// </summary>
    [UnitCategory("glTF/Interactivity")]
    [UnitTitle("Get glTF Asset Version")]
    public class GltfAssetVersionUnit : Unit
    {
        [DoNotSerialize]
        public ValueOutput majorVersion { get; private set; }

        [DoNotSerialize]
        public ValueOutput minorVersion { get; private set; }

        protected override void Definition()
        {
            // Best-effort constants for in-editor play mode; the real values are provided by the
            // KHR_interactivity runtime of the viewer that presents the exported asset.
            majorVersion = ValueOutput<int>("majorVersion", _ => 2);
            minorVersion = ValueOutput<int>("minorVersion", _ => 0);
        }
    }
}
