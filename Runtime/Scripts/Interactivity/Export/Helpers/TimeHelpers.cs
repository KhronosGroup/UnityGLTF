using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public static class TimeHelpers
    {

        public enum GetTimeValueOption
        {
            DeltaTime,
            TimeSinceStartup
        }

        /// <summary>
        /// This will ensure we don't get a NaN value when the time is not available.
        /// </summary>
        /// <param name="exporter"></param>
        /// <param name="valueOption"></param>
        /// <param name="value"></param>
        public static void AddTickNode(INodeExporter exporter, GetTimeValueOption valueOption, out ValueOutRef value)
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
            var tickNode = exporter.CreateNode<Event_OnTickNode>();
            
            var isNaNNode = exporter.CreateNode<Math_IsNaNNode>();

            isNaNNode.ValueIn(Math_IsNaNNode.IdValueA).ConnectToSource(tickNode.ValueOut(socketName));
            
            var selectNode = exporter.CreateNode<Math_SelectNode>();
            selectNode.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(isNaNNode.FirstValueOut());
            value = selectNode.FirstValueOut();

            selectNode.ValueIn(Math_SelectNode.IdValueB).ConnectToSource(tickNode.ValueOut(socketName));

            selectNode.ValueInConnection[Math_SelectNode.IdValueA] = new GltfInteractivityNode.ValueSocketData()
            {
                Value = 0f,
                Type = GltfTypes.TypeIndexByGltfSignature("float"),
            };
 

            value.ExpectedType(ExpectedType.Float);
        }
        
    }
}