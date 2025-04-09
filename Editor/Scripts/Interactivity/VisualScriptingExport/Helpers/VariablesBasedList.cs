using System;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class VariableBasedListFromUnit : VariableBasedList
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

    public class VariableBasedListFromGraphVariable : VariableBasedList
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
    
}