using Unity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting

{
    internal static class ValueInputHelper
    {
        public static bool TryGetValueInput(IUnitPortCollection<ValueInput> inputPorts, string key, out ValueInput valueInput)
        {
            valueInput = null;
            foreach (var inputPort in inputPorts)
            {
                var inputPortKey = inputPort.key;
                var hasSpecialSign = inputPort.key.StartsWith("%") || inputPort.key.StartsWith("&");
                if (hasSpecialSign)
                {
                    inputPortKey = inputPortKey.Substring(1);
                }
                    
                if (inputPortKey == key)
                {
                    valueInput = inputPort;
                    return true;
                }
            }

            return false;
        }   
    }
}