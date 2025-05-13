using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GetMemberUnitExport : IUnitExporterProvider, IMemberUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        public IEnumerable<(Type type, string member, MemberAccess access)> SupportedMembers => _memberExportRegister.SelectMany( x => x.Value.member.Select( m => (x.Key, m.Key, MemberAccess.Get)));
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GetMemberUnitExport());
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

            if (GetMemberGenericStaticValueExporter.CanBeExported(declaringType, memberName))
                return true;
            
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

            var getMember = unit as GetMember;
            if (getMember.member.info.IsStatic())
            {
                // Fallback to export static values as Variables, e.g.: Mathf.Rad2Deg
                return new GetMemberGenericStaticValueExporter();
            }
            
            return null;
        }
    }
    
    public class GetMemberGenericStaticValueExporter: IUnitExporter
    {
        public Type unitType { get; }

        public static bool CanBeExported(Type declaringType, string memberName)
        {
            var field = declaringType.GetField(memberName);
            if (field != null && field.IsStatic())
                return field.GetValue(null) != null;
            else
            {
                var property = declaringType.GetProperty(memberName);
                if (property == null)
                    return false;

                if (!property.IsStatic())
                    return false;
                
                return property.GetValue(null) != null;
            }
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;

            var varName = unit.member.name + "_" + unit.member.declaringType.Name;
            
            object value = null;
            object rawValue = null;
            var field = unit.member.declaringType.GetField(unit.member.name);
            
            if (field != null && field.IsStatic())
                rawValue = field.GetValue(null);
            else
            {
                var property = unit.member.declaringType.GetProperty(unit.member.name);
                if (property == null)
                    return false;
                
                if (!property.IsStatic())
                    return false;
                
                rawValue = property.GetValue(null);
            }

            if (rawValue == null)
                return false;
            
            if (rawValue is Double d)
                value = (float)d;
            else
            if (rawValue is long l)
                value = (int)l;
            else
            if (rawValue is byte b)
                value = (int)b;
            else
                value = rawValue;
            
            var gltfTypeIndex = GltfTypes.TypeIndex(value.GetType());
            if (gltfTypeIndex == -1)
            {
                Debug.LogError("Unsupported type to get static value: " + value.GetType()+ " from " + unit.member.declaringType);
                UnitExportLogging.AddErrorLog(unit, "Unsupported type: "+value.GetType().ToString());
                // Unsupported type
                return false;
            }
            var node = unitExporter.CreateNode<Variable_GetNode>();

            var variableIndex = unitExporter.vsExportContext.AddVariableWithIdIfNeeded(varName, value, VariableKind.Scene, gltfTypeIndex);
            node.OutputValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(gltfTypeIndex);
            
            node.Configuration["variable"].Value = variableIndex;
            
            unitExporter.MapValueOutportToSocketName(unit.value, Variable_GetNode.IdOutputValue, node); 
            return true;
        }
    }
}