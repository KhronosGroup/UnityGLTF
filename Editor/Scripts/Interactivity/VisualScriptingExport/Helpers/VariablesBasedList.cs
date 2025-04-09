using System;
using Unity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class VariableBasedListFromUnit : VariableBasedListExporter
    {
        public Unit listCreatorUnit;

        public VisualScriptingExportContext.ExportGraph listCreatorGraph;
        
        public VariableBasedListFromUnit(Unit listCreatorUnit,
            VisualScriptingExportContext context, string listId, int capacity, int gltfType) : base(context, listId,
            capacity, gltfType)
        {
            this.listCreatorUnit = listCreatorUnit;
        }
    }

    public class VariableBasedListFromGraphVariable : VariableBasedListExporter
    {
        public VariableDeclaration varDeclarationSource;
        
        public VisualScriptingExportContext.ExportGraph listCreatorGraph;
        
        public VariableBasedListFromGraphVariable(VariableDeclaration varDeclarationSource,
            VisualScriptingExportContext context, string listId, int capacity, int gltfType) : base(context, listId,
            capacity, gltfType)
        {
            this.varDeclarationSource = varDeclarationSource;
        }
    }

    public class VariableBasedListExporter : VariableBasedList
    {
        public ValueOutRef getCountNodeSocket;
        public FlowInRef setValueFlowIn;
        
        public VariableBasedListExporter(VisualScriptingExportContext context, string listId, int capacity, int gltfType) : base(context, listId,
            capacity, gltfType) {}

    }
    
}