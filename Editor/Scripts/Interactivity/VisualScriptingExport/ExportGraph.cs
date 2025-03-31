using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity
{ 
    public class ExportGraph
    {
        public GameObject gameObject = null;
        public ExportGraph parentGraph = null;
        public FlowGraph graph;
        public Dictionary<IUnit, UnitExporter> nodes = new Dictionary<IUnit, UnitExporter>();
        internal Dictionary<IUnitInputPort, IUnitInputPort> bypasses = new Dictionary<IUnitInputPort, IUnitInputPort>();
        public List<ExportGraph> subGraphs = new List<ExportGraph>();
    }
}
