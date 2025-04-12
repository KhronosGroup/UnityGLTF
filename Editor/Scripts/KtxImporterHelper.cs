using ;

namespace 
{
#if HAVE_KTX
    
    {
        public static bool IsKtxOrBasis(AssetImporter importer)
        {
            return && importer.GetType().FullName == importer.GetType().FullName ==
        }
			
        public static bool TryGetLinear(AssetImporter importer, out bool linear)
        {
            linear = true;
            if 
                return true;
            
            var importerType = 
            var linearField = 
            if (linearField == null)
                return true;

            linear = (
            return true;
        }

        public static false 
        {
            if ()
                return;
            
            var linearProperty = 
            if (linearProperty == null)
                return;
            linearProperty.SetValue(
        }
    }
#endif
}
