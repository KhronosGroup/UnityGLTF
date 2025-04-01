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

        public static void AddTickNode(UnitExporter unitExporter, GetTimeValueOption valueOption, out GltfInteractivityUnitExporterNode.ValueOutputSocketData value)
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
     
            unitExporter.MapInputPortToSocketName(socketName, tickNode,
                "a", isNaNNode);
            
            var selectNode = unitExporter.CreateNode(new Math_SelectNode());
            unitExporter.MapInputPortToSocketName("value", isNaNNode, Math_SelectNode.IdCondition, selectNode);
            value = selectNode.FirstValueOut();

            unitExporter.MapInputPortToSocketName(socketName, tickNode, 
                Math_SelectNode.IdValueB, selectNode);
            selectNode.ValueInConnection[Math_SelectNode.IdValueA] = new GltfInteractivityUnitExporterNode.ValueSocketData()
            {
                Value = 0f,
                Type = GltfTypes.TypeIndexByGltfSignature("float"),
            };
 

            value.ExpectedType(ExpectedType.Float);
        }
        
    }
}