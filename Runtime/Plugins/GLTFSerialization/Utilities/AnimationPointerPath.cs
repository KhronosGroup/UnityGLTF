namespace GLTF.Utilities
{
   public class PointerPath
    {
        public enum PathElement { Root, RootExtension, Index, Extension, Child, Property }
        public PathElement PathElementType { get; private set; } = PathElement.Root;
        public int index { get; private set; } = -1;
        public string elementName { get; private set; } = "";

        public bool isValid { get; internal set; } = false;
        
        public PointerPath next { get; private set; } = null;

        public string ExtractPath()
        {
            return elementName+ (next != null ? "/" + next.ExtractPath() : "");
        }
        
        public PointerPath FindNext(PathElement pathElementType)
        {
            if (this.PathElementType == pathElementType)
                return this;
            
            if (next == null)
                return null;
            return next.FindNext(pathElementType);
        }
        
        private PointerPath()
        {
            isValid = true;
        }

        public PointerPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return;
            
            var splittedPath = fullPath.Split('/');
            var pathIndex = 0;

            if (string.IsNullOrEmpty(splittedPath[0]))
                pathIndex++;
            
            if (splittedPath.Length <= pathIndex)
                return;
            
            isValid = true;

            elementName = splittedPath[pathIndex];
            PathElementType = PathElement.Root;

            string GetCurrentAsString()
            {
                return splittedPath[pathIndex];
            }

            bool GetCurrentAsInt(out int result)
            {
                return int.TryParse(splittedPath[pathIndex], out result);
            }            
            
            PointerPath TravelHierarchy()
            {
                pathIndex++;
                if (pathIndex >= splittedPath.Length)
                    return null;
                
                var result = new PointerPath();
                
                if (GetCurrentAsInt(out int index))
                {
                    result.index = index;
                    result.PathElementType = PathElement.Index;
                    result.elementName = index.ToString();
                    result.next = TravelHierarchy();
                    return result;
                }
                
                result.elementName = GetCurrentAsString();
                if (result.elementName == "extensions")
                    result.PathElementType = PathElement.Extension;
                else
                    result.PathElementType = (pathIndex == splittedPath.Length-1) ? PathElement.Property : PathElement.Child;
                if ((pathIndex < splittedPath.Length))
                    result.next = TravelHierarchy();
                return result;
            }
            
            if (elementName == "extensions")
            {
                pathIndex++;
                if (pathIndex < splittedPath.Length)
                {
                    elementName = GetCurrentAsString();
                    PathElementType = PathElement.RootExtension;
                    next = TravelHierarchy();
                }
            }
            else
            {
                next = TravelHierarchy();
            }
            
        }
    }
   
}