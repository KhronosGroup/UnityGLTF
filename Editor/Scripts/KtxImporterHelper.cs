using UnityEditor;

namespace UnityGLTF
{
#if HAVE_KTX
    internal static class KtxImporterHelper
    {
        public static bool IsKtxOrBasis(AssetImporter importer)
        {
            return importer && importer.GetType().FullName == "KtxUnity.KtxImporter" || importer.GetType().FullName == "KtxUnity.BasisImporter";
        }
			
        public static bool TryGetLinear(AssetImporter importer, out bool linear)
        {
            linear = false;
            if (!IsKtxOrBasis(importer))
                return false;
            
            var importerType = importer.GetType();
            var linearField = importerType.GetField("linear");
            if (linearField == null)
                return false;

            linear = (bool)linearField.GetValue(importer);
            return true;
        }

        public static void SetLinear(AssetImporter importer, bool linear)
        {
            if (!IsKtxOrBasis(importer))
                return;
            
            var linearProperty = importer.GetType().GetField("linear");
            if (linearProperty == null)
                return;
            linearProperty.SetValue(importer, linear);
				
            EditorUtility.SetDirty(importer);
        }
    }
#endif
}