using System.Collections;

namespace GLTF.Utilities
{
    public class AnimationPointerPathHierarchy
    {
        public enum ElementTypeOptions { Root, Extension, Index, Child, Property }
        public ElementTypeOptions elementType { get; private set; } = ElementTypeOptions.Root;
        public int index { get; private set; } = -1;
        public string elementName { get; private set; } = "";

        public AnimationPointerPathHierarchy next { get; private set; }= null;

        public AnimationPointerPathHierarchy FindElement(ElementTypeOptions elementType)
        {
            if (this.elementType == elementType)
                return this;
            
            if (next == null)
                return null;
            return next.FindElement(elementType);
        }
        
        public static AnimationPointerPathHierarchy CreateHierarchyFromFullPath(string fullPath)
        {
            var path = new PathResolver(fullPath.Remove(0,1));
            
            var result = new AnimationPointerPathHierarchy();
            result.elementName = path.GetCurrentAsString();
            result.elementType = ElementTypeOptions.Root;

            AnimationPointerPathHierarchy TravelHierarchy(PathResolver path)
            {
                if (!path.MoveNext())
                    return null;
                
                var result = new AnimationPointerPathHierarchy();
                if (path.GetCurrentAsInt(out int index))
                {
                    result.index = index;
                    result.elementType = ElementTypeOptions.Index;
                    result.elementName = index.ToString();
                    result.next = TravelHierarchy(path);
                    return result;
                }
                
                result.elementName = path.GetCurrentAsString();
                result.elementType = path.IsLast() ? ElementTypeOptions.Property : ElementTypeOptions.Child;
                if (!path.IsLast())
                    result.next = TravelHierarchy(path);
                return result;
            }
            
            if (result.elementName == "extensions")
            {
                if (path.MoveNext())
                {
                    result.elementName = path.GetCurrentAsString();
                    result.elementType = ElementTypeOptions.Extension;
                    result.next = TravelHierarchy(path);
                }
            }
            else
            {
                result.next = TravelHierarchy(path);
            }

            return result;
        }
    }
    public class PathResolver : IEnumerator
    {
        private string[] _splittedPath;
        private int currentIndex;
        
        public PathResolver (string path)
        {
            _splittedPath = path.Split("/");
            currentIndex = 0;
        }
        
        public bool IsLast()
        {
            return currentIndex == _splittedPath.Length - 1;
        }
        
        public string GetCurrentAsString()
        {
            return _splittedPath[currentIndex];
        }

        public bool GetCurrentAsInt(out int result)
        {
            return int.TryParse(_splittedPath[currentIndex], out result);
        }
        
        public bool MoveNext()
        {
            currentIndex++;
            return currentIndex < _splittedPath.Length;
        }

        public void Reset()
        {
            currentIndex = 0;
        }

        public object Current
        {
            get
            {
                if (currentIndex < _splittedPath.Length)
                {
                    return _splittedPath[currentIndex];
                }

                return null;
            }
        }
    }
}