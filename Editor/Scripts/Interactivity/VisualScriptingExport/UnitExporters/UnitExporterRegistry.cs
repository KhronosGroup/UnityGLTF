using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Don't use this interface directly, use IUnitExporter or IUnitExporterProvider instead.
    /// </summary>
    public interface IUnitTypeExporter
    {
        System.Type unitType { get; }
    }

    public interface IUnitExporterProvider : IUnitTypeExporter
    {
        public IUnitExporter GetExporter(IUnit unit);
    }
    
    [Flags]
    public enum MemberAccess
    {
        Get = 4,
        Set = 2,
        Invoke = 1,
        None = 0
    }
    
    public interface IMemberUnitExporter : IUnitTypeExporter
    {
        IEnumerable<(Type type, string member, MemberAccess access)> SupportedMembers { get; }
    }
    
    public interface ICoroutineWait { }

    public interface ICoroutineAwaiter { }
    
    public enum ExportPriority
    {
        First = 0,
        Default = 1,
        Last = 2
    }
    
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class UnitExportPriority : Attribute
    {
        public ExportPriority priority { get; private set; }
        
        public UnitExportPriority(ExportPriority priority)
        {
            this.priority = priority;
        }
    }
    
    public interface IUnitExporter : IUnitTypeExporter
    {
        bool InitializeInteractivityNodes(UnitExporter unitExporter);
    }
    
    public static class UnitExporterRegistry
    {
        private static Dictionary<System.Type, IUnitTypeExporter> _exportRegistry =
            new Dictionary<System.Type, IUnitTypeExporter>();

        public static ReadOnlyDictionary<System.Type, IUnitTypeExporter> Exporters
        { 
            get => new ReadOnlyDictionary<Type, IUnitTypeExporter>(_exportRegistry);
        }

        // TODO: find a better way to register export converters, a more static way aproach would be better. No need for instances her
        public static void RegisterExporter(IUnitTypeExporter nodeConvert)
        {
            if (_exportRegistry.ContainsKey(nodeConvert.unitType))
            {
                Debug.LogError("A UnitExporter is already registered for Unit Type: " + nodeConvert.unitType.ToString() +" Trying to register: " + nodeConvert.GetType().ToString());
                return;
            }

            _exportRegistry.Add(nodeConvert.unitType, nodeConvert);
        }

        public static bool HasUnitExporter(IUnit unit)
        {
            var directlyExported = _exportRegistry.ContainsKey(unit.GetType());
            if (unit is GetMember || unit is SetMember || unit is InvokeMember || unit is Expose || unit is CreateStruct)
                directlyExported = false;
            
            var invokeExported = unit is InvokeMember invokeMember &&
                                 InvokeUnitExport.HasInvokeConvert(invokeMember.member?.declaringType, invokeMember.member?.name);
            var setMemberExported = unit is SetMember setMember &&
                                    SetMemberUnitExport.HasMemberConvert(setMember.member?.declaringType, setMember.member?.name);
            var getMemberExported = unit is GetMember getMember &&
                                    GetMemberUnitExport.HasMemberConvert(getMember.member?.declaringType, getMember.member?.name);

            var createStructExported = unit is CreateStruct createStruct &&
                                    CreateStructsUnitExport.HasConvert(createStruct.type);

            var exposeExported = unit is Expose expose && ExposeUnitExport.HasConvert(expose.type);
            return createStructExported || directlyExported || invokeExported || setMemberExported || getMemberExported || exposeExported;
        }

        public static IUnitExporter GetUnitExporter(IUnit unit)
        {
            var unitType = unit.GetType();
            if (unitType == typeof(Literal))
            {
                // Only contains a value.
                return null;
            }

            if (!_exportRegistry.ContainsKey(unitType))
                return null;

            var converter = _exportRegistry[unitType];
            if (converter == null)
                return null;

            if (converter is IUnitExporterProvider dynamic)
                return dynamic.GetExporter(unit);

            return converter as IUnitExporter;
        }

        public static UnitExporter CreateUnitExporter(VisualScriptingExportContext exportContext, IUnit unit)
        {
            var converter = GetUnitExporter(unit);
            if (converter == null)
            {
                return null;
            }

            return new UnitExporter(exportContext, converter, unit);
        }
    }
}