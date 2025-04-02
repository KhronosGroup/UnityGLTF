using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ExposeUnitExport : IUnitExporterProvider
    {
        public Type unitType
        {
            get => typeof(Expose);
        }
        
        private static Dictionary<Type, (IUnitExporter converter, string[] supportedMembers)> _exposeExportRegister = new Dictionary<Type, (IUnitExporter, string[])>();
        
        public IEnumerable<(Type, string[] members)> SupportedMembers => _exposeExportRegister.Select(pair => (pair.Key, pair.Value.supportedMembers));
      
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ExposeUnitExport());
        }
        
        public static void RegisterExposeConvert(Type declaringType, IUnitExporter unitExporter, params string[] supportedMembers)
        {
            _exposeExportRegister.Add(declaringType, new (unitExporter, supportedMembers));
        }
        
        public static bool HasConvert(Type declaringType)
        {
            return _exposeExportRegister.ContainsKey(declaringType);
        }
        
        public static string[] GetSupportedMembers(Type declaringType)
        {
            if (_exposeExportRegister.TryGetValue(declaringType, out var exposeConvert))
                return exposeConvert.supportedMembers;

            return null;
        }
        
        public IUnitExporter GetExporter(IUnit unit)
        {
            var exposeUnit = unit as Expose;
            if (_exposeExportRegister.TryGetValue(exposeUnit.type, out var exposeConvert))
                return exposeConvert.converter;

            return null;
        }
    }
}