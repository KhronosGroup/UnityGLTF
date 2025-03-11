using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class CreateStructsUnitExport : IUnitExporterProvider
    {
        public Type unitType
        {
            get => typeof(CreateStruct);
        }
        
        private static Dictionary<Type, IUnitExporter> _createStructRegister = new Dictionary<Type, IUnitExporter>();
        
        public IEnumerable<Type> SupportedTypes => _createStructRegister.Keys;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new CreateStructsUnitExport());
        }
        
        public static void RegisterCreateStructConvert(Type declaringType, IUnitExporter unitExporter)
        {
            _createStructRegister.Add(declaringType, unitExporter);
        }
        
        public static bool HasConvert(Type declaringType)
        {
            return _createStructRegister.ContainsKey(declaringType);
        }
        
        public IUnitExporter GetExporter(IUnit unit)
        {
            var createStruct = unit as CreateStruct;
            if (_createStructRegister.TryGetValue(createStruct.type, out var exporter))
            {
                return exporter;
            }

            return null;
        }
    }
}