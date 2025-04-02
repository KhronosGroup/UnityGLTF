using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public static class SupportedUnitExports
    {

        public static void LogSupportedUnits()
        {
            Dictionary<Type, Dictionary<string, MemberAccess>> memberAccess = new Dictionary<Type, Dictionary<string, MemberAccess>>();
            List<string> units = new List<string>();
            IEnumerable<Type> supportedStructsCreation = null;
            IEnumerable<(Type type, string[] members)> exposedMembers = null;
            
            var sb = new StringBuilder();
            
            foreach (var exporter in UnitExporterRegistry.Exporters)
            {
                if (exporter.Value is IMemberUnitExporter memberExporter)
                {
                    foreach (var m in memberExporter.SupportedMembers)
                    {
                        if (memberAccess.TryGetValue(m.type, out var member))
                        {
                            if (member.ContainsKey(m.member))
                                member[m.member] |= m.access;
                            else
                                member[m.member] = m.access;
                        }
                        else
                        {
                            member = new Dictionary<string, MemberAccess>();
                            member[m.member] = m.access;
                            memberAccess[m.type] = member;
                        }
                    }
                }
                else
                if (exporter.Value is CreateStructsUnitExport structExporter)
                {
                    supportedStructsCreation = structExporter.SupportedTypes;

                }
                else
                if (exporter.Value is ExposeUnitExport exposeExporter)
                {
                    exposedMembers = exposeExporter.SupportedMembers;
                }
                else
                {
                    units.Add(exporter.Key.Name);
                }
                
            }
            sb.AppendLine("UnityGltf Interactivity - Supported Visual Scripting Units:");
            
            sb.AppendLine("<b>Visual Scripting Units:</b>");
            foreach (var unit in units.OrderBy( u => u))
                sb.AppendLine($"\t{unit}");

            if (supportedStructsCreation != null)
            {
                sb.AppendLine("<b>Struct creation:</b>");
                foreach (var structType in supportedStructsCreation)
                    sb.AppendLine($"\t{structType}");
            }
            
            if (exposedMembers != null)
            {
                sb.AppendLine("<b>Exposed Units:</b>");
                foreach (var member in exposedMembers.OrderBy(m => m.type.FullName))
                {
                    sb.AppendLine($"\t{member.type.ToString()}");
                    foreach (var m in member.members.OrderBy( m => m))
                    {
                        sb.AppendLine($"\t\t.{m}");
                    }
                }
            }
            
            sb.AppendLine("<b>Supported Members:</b>");
            foreach (var member in memberAccess.OrderBy( t => t.Key.FullName))
            {
                sb.AppendLine($"\t{member.Key.ToString()}");
                foreach (var m in member.Value.OrderBy( m => m.Key))
                {
                    sb.AppendLine($"\t\t.{m.Key} ({m.Value.ToString()})");
                }
            }
            Debug.Log(sb.ToString());
        }
    }
}