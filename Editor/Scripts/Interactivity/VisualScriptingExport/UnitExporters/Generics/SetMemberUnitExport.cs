using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SetMemberUnitExport : IUnitExporterProvider, IMemberUnitExporter 
    {
        public Type unitType { get => typeof(SetMember); }

        public IEnumerable<(Type type, string member, MemberAccess access)> SupportedMembers => _memberExportRegister.SelectMany( x => x.Value.member.Select( m => (x.Key, m.Key, MemberAccess.Set)));
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SetMemberUnitExport());
        }
        
        protected class TypeMember
        {
            public Dictionary<string, IUnitExporter> member = new Dictionary<string, IUnitExporter>();
        }
        
        protected static Dictionary<Type, TypeMember> _memberExportRegister = new Dictionary<Type, TypeMember>();

        public static void RegisterMemberExporter(Type declaringType, string memberName, IUnitExporter unitExporter)
        {
            if (!_memberExportRegister.TryGetValue(declaringType, out var typeMember))
            {
                typeMember = new TypeMember();
                _memberExportRegister[declaringType] = typeMember;
            }
            typeMember.member[memberName] = unitExporter;
        }
        
        public static bool HasMemberConvert(Type declaringType, string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return false;
            
            if (_memberExportRegister.TryGetValue(declaringType, out var typeInvokes))
            {
                return typeInvokes.member.ContainsKey(memberName);
            }

            return false;
        }

        public virtual IUnitExporter GetExporter(IUnit unit)
        {
            var member = unit as MemberUnit;
            if (_memberExportRegister.TryGetValue(member.member.declaringType, out var memberExporters))
            {
                if (memberExporters.member.TryGetValue(member.member.name, out var exporter))
                    return exporter;
            }

            return null;
        }
    }
}