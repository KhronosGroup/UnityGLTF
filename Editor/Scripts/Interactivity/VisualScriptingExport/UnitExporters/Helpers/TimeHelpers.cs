using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class TimeHelpers
    {

        public enum GetTimeValueOption
        {
            DeltaTime,
            TimeSinceStartup
        }

        public static void AddTickNode(UnitExporter unitExporter, GetTimeValueOption valueOption, out ValueOutRef value)
        {
            var socketName = Event_OnTickNode.IdOutTimeSinceLastTick;
            switch (valueOption)
            {
                case GetTimeValueOption.DeltaTime:
                    socketName = Event_OnTickNode.IdOutTimeSinceLastTick;
                    break;
                case GetTimeValueOption.TimeSinceStartup:
                    socketName = Event_OnTickNode.IdOutTimeSinceStart;
                    break;
                
            }
            var tickNode = unitExporter.CreateNode(new Event_OnTickNode());
            
            var isNaNNode = unitExporter.CreateNode(new Math_IsNaNNode());

            isNaNNode.ValueIn(Math_IsNaNNode.IdValueA).ConnectToSource(tickNode.ValueOut(socketName));
            
            var selectNode = unitExporter.CreateNode(new Math_SelectNode());
            selectNode.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(isNaNNode.FirstValueOut());
            value = selectNode.FirstValueOut();

            selectNode.ValueIn(Math_SelectNode.IdValueB).ConnectToSource(tickNode.ValueOut(socketName));

            selectNode.ValueInConnection[Math_SelectNode.IdValueA] = new GltfInteractivityUnitExporterNode.ValueSocketData()
            {
                Value = 0f,
                Type = GltfTypes.TypeIndexByGltfSignature("float"),
            };
 

            value.ExpectedType(ExpectedType.Float);
        }
        
    }
}