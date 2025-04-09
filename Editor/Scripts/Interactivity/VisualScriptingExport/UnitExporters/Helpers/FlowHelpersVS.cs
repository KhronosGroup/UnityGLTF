using System.Collections.Generic;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class FlowHelpersVS : FlowHelpers
    {
        public static bool RequiresCoroutines(ControlInput input, out ControlInput coroutineControlInput)
        {
            coroutineControlInput = null;
            var visited = new HashSet<IUnit>();
            var stack = new Stack<ControlInput>();
            stack.Push(input);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (visited.Contains(current.unit))
                    continue;

                visited.Add(current.unit);

                if (current.requiresCoroutine)
                {
                    coroutineControlInput = current;
                    return true;

                }

                foreach (var controlOutput in current.unit.controlOutputs)
                {
                    if (!controlOutput.hasValidConnection)
                        continue;

                    stack.Push(controlOutput.connection.destination);
                }
            }

            return false;
        }
    }
}