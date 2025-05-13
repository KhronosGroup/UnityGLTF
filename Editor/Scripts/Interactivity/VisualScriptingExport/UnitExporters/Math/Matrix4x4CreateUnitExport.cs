using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Matrix4x4CreateUnitExport : IUnitExporter
    {
        private static readonly string[] InputNames = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"};

        public Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), ".ctor", new Matrix4x4CreateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var extracts = new GltfInteractivityExportNode[4];
            for (int i = 0; i < 4; i++)
            {
                extracts[i] = unitExporter.CreateNode<Math_Extract4Node>();
                extracts[i].ValueIn(Math_Extract4Node.IdValueIn).MapToInputPort(unit.valueInputs[i]);
            }

            var createMatrix = unitExporter.CreateNode<Math_Combine4x4Node>();

            int index = 0;

            for (int iExtracts = 0; iExtracts < 4; iExtracts++)
            {
                for (int iSocket = 0; iSocket < 4; iSocket++)
                {
                    createMatrix.ValueIn(InputNames[index]).ConnectToSource(extracts[iExtracts].ValueOut(iSocket.ToString()));
                    index++;
                }
            }
            return true;
        }
    }
}