using System;
using Unity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class VariableBasedListFromUnit : VariableBasedList
    {
        public Unit listCreatorUnit;

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

        public VariableBasedListFromGraphVariable(VariableDeclaration varDeclarationSource,
            VisualScriptingExportContext context, string listId, int capacity, int gltfType) : base(context, listId,
            capacity, gltfType)
        {
            this.varDeclarationSource = varDeclarationSource;
        }
    }
    
    public class VariableBasedList
    {
        public readonly VisualScriptingExportContext Context;
        public readonly int Capacity;
        public readonly int StartIndex = 0;
        public readonly int EndIndex = 0;
        public readonly string ListId;
        public readonly int CountVarId;
        public readonly int CapacityVarId;
        public readonly int ListIndex;
        public readonly int CurrentIndexVarId;
        public readonly int ValueToSetVarId;

        public GltfInteractivityUnitExporterNode.ValueOutputSocketData getCountNodeSocket;
        public GltfInteractivityUnitExporterNode.ValueOutputSocketData getValueNodeSocket;
        public GltfInteractivityUnitExporterNode.FlowInSocketData setValueFlowIn;

        public VisualScriptingExportContext.ExportGraph listCreatorGraph;

        public VariableBasedList(VisualScriptingExportContext context, string listId, int capacity, int gltfType)
        {
            Capacity = capacity;
            Context = context;
            ListId = listId;

            CurrentIndexVarId = Context.AddVariableWithIdIfNeeded($"VARLIST_{listId}_CurrentIndex", 0, VariableKind.Scene, typeof(int));
            ValueToSetVarId = Context.AddVariableWithIdIfNeeded($"VARLIST_{listId}_ValueToSet", 0, VariableKind.Scene, gltfType);

            ListIndex = Context.variables.Count;

            CountVarId = Context.AddVariableWithIdIfNeeded($"VARLIST_{listId}_Count", 0, VariableKind.Scene, typeof(int));
            CapacityVarId = Context.AddVariableWithIdIfNeeded($"VARLIST_{listId}_Capacity", Capacity, VariableKind.Scene, typeof(int));

            StartIndex = -1;
            EndIndex = -1;
            for (int i = 0; i < capacity; i++)
            {
                EndIndex = Context.AddVariableWithIdIfNeeded($"VARLIST_{listId}_{i}", null, VariableKind.Scene, gltfType);
                if (StartIndex == -1)
                    StartIndex = EndIndex;
            }
        }

        public void ClearList()
        {
            Context.variables[CountVarId].Value = 0;
        }

        public void AddItem(object value)
        {
            if (Context.variables[CountVarId].Value is int count)
            {
                if (count >= Capacity)
                    throw new ArgumentException("List is full. Current Capacity: " + Capacity);

                Context.variables[StartIndex + count].Value = value;
                Context.variables[CountVarId].Value = count + 1;
            }
        }

        public void SetItem(int index, object value)
        {
            if (Context.variables[CountVarId].Value is int count)
            {
                if (index < 0 || index > count - 1)
                    throw new IndexOutOfRangeException("Index out of range for list. Can't set item with index: " +
                                                       index);

                Context.variables[StartIndex + index].Value = value;
            }
        }
    }
}