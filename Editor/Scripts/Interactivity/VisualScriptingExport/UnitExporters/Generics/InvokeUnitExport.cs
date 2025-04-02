using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class InvokeUnitExport : IUnitExporterProvider, IMemberUnitExporter
    {
        public Type unitType
        {
            get => typeof(InvokeMember);
        }

        private class TypeInvokes
        {
            public Dictionary<string, IUnitExporter> invokes = new Dictionary<string, IUnitExporter>();
        }
                
        private static Dictionary<Type, TypeInvokes> _invokeExportRegister = new Dictionary<Type, TypeInvokes>();
        public IEnumerable<(Type type, string member, MemberAccess access)> SupportedMembers => _invokeExportRegister.SelectMany( x => x.Value.invokes.Select( m => (x.Key, m.Key, MemberAccess.Invoke)));
        
        public static void RegisterInvokeExporter(Type type, string invokeName, IUnitExporter unitExporter)
        {
            if (!_invokeExportRegister.TryGetValue(type, out var typeInvokes))
            {
                typeInvokes = new TypeInvokes();
                _invokeExportRegister[type] = typeInvokes;
            }
            typeInvokes.invokes[invokeName] = unitExporter;
        }
        
        public static bool HasInvokeConvert(Type type, string invokeName)
        {
            if (_invokeExportRegister.TryGetValue(type, out var typeInvokes))
            {
                return typeInvokes.invokes.ContainsKey(invokeName);
            }

            return false;
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new InvokeUnitExport());
        }
        
        public IUnitExporter GetExporter(IUnit unit)
        {
            var invokeMember = unit as InvokeMember;

            if (_invokeExportRegister.TryGetValue(invokeMember.member.declaringType, out var typeInvokes))
            {
                if (typeInvokes.invokes.TryGetValue(invokeMember.member.name, out var exportNodeConvert))
                {
                    return exportNodeConvert;
                }
            }
            
            return null;
        }
    }
}