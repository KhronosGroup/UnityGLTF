using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;
using Random = UnityEngine.Random;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class RandomRangeUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Random), nameof(Random.Range), new RandomRangeUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var randomNode = unitExporter.CreateNode<Math_RandomNode>();

            if (unit.valueInputs[0].type == typeof(int))
            {
                // integer Random and Max Exclusive
                
                // Sub input0 from input1
                var subNode = unitExporter.CreateNode<Math_SubNode>();
                subNode.ValueIn("a").SetType(TypeRestriction.LimitToInt).MapToInputPort(unit.valueInputs[1]);
                subNode.ValueIn("b").SetType(TypeRestriction.LimitToInt).MapToInputPort(unit.valueInputs[0]);
                
                // Mul random with sub result
                var mulNode = unitExporter.CreateNode<Math_MulNode>();
                mulNode.ValueIn("a").ConnectToSource(randomNode.FirstValueOut());
                mulNode.ValueIn("b").ConnectToSource(subNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat);

                // Floor the result
                var floorNode = unitExporter.CreateNode<Math_FloorNode>();
                floorNode.ValueIn("a").ConnectToSource(mulNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat);
                
                // Convert to int
                var toIntNode = unitExporter.CreateNode<Type_FloatToIntNode>();
                toIntNode.ValueIn("a").ConnectToSource(floorNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat);

                // Add the result to input0
                var addNode = unitExporter.CreateNode<Math_AddNode>();
                addNode.ValueIn("a").ConnectToSource(toIntNode.FirstValueOut()).SetType(TypeRestriction.LimitToInt);
                addNode.ValueIn("b").SetType(TypeRestriction.LimitToInt).MapToInputPort(unit.valueInputs[0]);
                addNode.FirstValueOut().MapToPort(unit.result).ExpectedType(ExpectedType.Int);
            }
            else
            {
                // float Random and Max Inclusive

                if (unitExporter.IsInputLiteralOrDefaultValue(unit.valueInputs[0], out var valueA)
                    && unitExporter.IsInputLiteralOrDefaultValue(unit.valueInputs[1], out var valueB)
                    && valueA is float a && valueB is float b && a == 0f && b == 1f)
                {
                    // Range is 0 to 1, we don't need a mix node here
                    randomNode.FirstValueOut().MapToPort(unit.result);
                }
                else
                {
                    var mixNode = unitExporter.CreateNode<Math_MixNode>();
                    mixNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat).MapToInputPort(unit.valueInputs[0]);
                    mixNode.ValueIn("b").SetType(TypeRestriction.LimitToFloat).MapToInputPort(unit.valueInputs[1]);
                    mixNode.ValueIn("c").ConnectToSource(randomNode.FirstValueOut()).SetType(TypeRestriction.LimitToFloat);
                    mixNode.FirstValueOut().ExpectedType(ExpectedType.Float).MapToPort(unit.result);
                }
            }
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}