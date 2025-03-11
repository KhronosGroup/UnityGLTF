using System.Collections.Generic;
using Unity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class UnitLogs
    {
        public readonly List<string> infos = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> errors = new List<string>();
        
        public string GetWarningsAsString()
        {
            return string.Join("\n", warnings);
        }
        
        public string GetErrorsAsString()
        {
            return string.Join("\n", errors);
        }
        
        public string GetInfosAsString()
        {
            return string.Join("\n", infos);
        }
        
        public bool HasErrors()
        {
            return errors.Count > 0;
        }
        
        public bool HasWarnings()
        {
            return warnings.Count > 0;
        }
        
        public bool HasInfos()
        {
            return infos.Count > 0;
        }
        
    }
    
    public static class UnitExportLogging
    {
        public static readonly Dictionary<IUnit, UnitLogs> unitLogMessages = new Dictionary<IUnit, UnitLogs>();
        
        public static void ClearLogs()
        {
            unitLogMessages.Clear();
        }
        
        public static void AddWarningLog(IUnit unit, string message)
        {
            if (!unitLogMessages.ContainsKey(unit))
            {
                unitLogMessages[unit] = new UnitLogs();
            }
            
            unitLogMessages[unit].warnings.Add(message);
        }
        
        public static void AddErrorLog(IUnit unit, string message)
        {
            if (!unitLogMessages.ContainsKey(unit))
            {
                unitLogMessages[unit] = new UnitLogs();
            }
            
            unitLogMessages[unit].errors.Add(message);
        }

    }
}