using UnityEditor;

namespace UnityGLTF
{
    /// <summary>
    /// Reads and sets the color space of a texture file that is imported by the WebP package (com.netpyoung.webp).
    /// It has its own importer (not a TextureImporter) with a "Linear" setting. Accessed by reflection, so there is
    /// no assembly reference to the package.
    /// </summary>
    internal static class WebPImporterHelper
    {
        private const string ImporterTypeName = "WebP.WebPImporter";
        private const string LinearFieldName = "Linear";

        public static bool IsWebP(AssetImporter importer)
        {
            return importer && importer.GetType().FullName == ImporterTypeName;
        }

        public static bool TryGetLinear(AssetImporter importer, out bool linear)
        {
            linear = false;
            if (!IsWebP(importer))
                return false;

            var linearField = importer.GetType().GetField(LinearFieldName);
            if (linearField == null)
                return false;

            linear = (bool)linearField.GetValue(importer);
            return true;
        }

        public static void SetLinear(AssetImporter importer, bool linear)
        {
            if (!IsWebP(importer))
                return;

            var linearField = importer.GetType().GetField(LinearFieldName);
            if (linearField == null)
                return;

            linearField.SetValue(importer, linear);
            EditorUtility.SetDirty(importer);
        }
    }
}
