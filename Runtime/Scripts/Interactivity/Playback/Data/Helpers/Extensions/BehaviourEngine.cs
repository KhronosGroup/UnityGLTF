namespace UnityGLTF.Interactivity.Playback.Extensions
{
    public static partial class ExtensionMethods
    {
        public static bool TryGetPointer<T>(this BehaviourEngine engine, string pointerString, BehaviourEngineNode engineNode, out Pointer<T> pointer)
        {
            pointer = default;

            if (!engine.TryGetPointer(pointerString, engineNode, out IPointer p))
                return false;

            pointer = (Pointer<T>)p;
            return true;
        }

        public static bool TryGetPointer(this BehaviourEngine engine, string pointerString, BehaviourEngineNode engineNode, out IReadOnlyPointer readOnlyPointer)
        {
            readOnlyPointer = default;

            if (!engine.TryGetPointer(pointerString, engineNode, out IPointer pointer))
                return false;

            readOnlyPointer = (IReadOnlyPointer)pointer;
            return true;
        }

        public static bool TryGetPointer<T>(this BehaviourEngine engine, string pointerString, BehaviourEngineNode engineNode, out ReadOnlyPointer<T> pointer)
        {
            pointer = default;

            if (!engine.TryGetPointer(pointerString, engineNode, out IReadOnlyPointer readOnlyPointer))
                return false;

            pointer = (ReadOnlyPointer<T>)readOnlyPointer;
            return true;
        }
    }
}