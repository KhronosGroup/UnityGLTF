using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SubGraphUnitExporter : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(SubgraphUnit);
        }

        private class SubGraphInputNode : IUnitExporter
        {
            public Type unitType {get => typeof(GraphInput); }
            public bool InitializeInteractivityNodes(UnitExporter unitExporter)
            {  
                return true;
            }
        }
        
        private class SubGraphOutputNode : IUnitExporter
        {
            public Type unitType {get => typeof(GraphOutput); }

            public bool InitializeInteractivityNodes(UnitExporter unitExporter)
            {
                return true;
            }
        }
        
        [InitializeOnLoadMethod]
        public static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SubGraphUnitExporter());
            
            // Register the subgraph input and output nodes, to avoid false Warning Logs
            UnitExporterRegistry.RegisterExporter(new SubGraphInputNode());
            UnitExporterRegistry.RegisterExporter(new SubGraphOutputNode());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SubgraphUnit;

            var subGraph = unit.nest.graph;
            
            var subGraphExport = unitExporter.vsExportContext.AddGraph(subGraph, unit);
            
            var subGraphInputUnit = subGraph.units.FirstOrDefault(u => u is GraphInput);
            var subGraphOutputUnit = subGraph.units.FirstOrDefault(u => u is GraphOutput);
            
            if (subGraphInputUnit != null)
            {
                foreach (var cInput in unit.controlInputs)
                {
                    if (!cInput.hasValidConnection)
                        continue;

                    var port = subGraphInputUnit.controlOutputs[cInput.key];
                    if (port != null && port.hasValidConnection)
                        unitExporter.ByPassFlow(cInput, unitExporter.Graph, port, subGraphExport);
                }

                foreach (var vInput in unit.valueInputs)
                {
                    if (!vInput.hasValidConnection)
                        continue;
                    
                    var port = subGraphInputUnit.valueOutputs[vInput.key];
                    if (port != null && port.hasValidConnection)
                       unitExporter.ByPassValue(vInput, unitExporter.Graph, port, subGraphExport);
                }
            }
            
            if (subGraphOutputUnit != null)
            {
                foreach (var cOutput in unit.controlOutputs)
                {
                    if (!cOutput.hasValidConnection)
                        continue;

                    var port = subGraphOutputUnit.controlInputs[cOutput.key];
                    if (port != null && port.hasValidConnection)
                        unitExporter.ByPassFlow(port, subGraphExport, cOutput, unitExporter.Graph);
                }
                
                foreach (var vOutput in unit.valueOutputs)
                {
                    if (!vOutput.hasValidConnection)
                        continue;

                    var port = subGraphOutputUnit.valueInputs[vOutput.key];
                    if (port != null && port.hasValidConnection)
                        unitExporter.ByPassValue(port, subGraphExport, vOutput, unitExporter.Graph);
                }
            }
            return true;
        }
    }
}