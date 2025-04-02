using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public class GltfInteractivityExportNode : GltfInteractivityNode
    {
        // This data will not be serialized
        public Dictionary<string, OutputValueSocketData> OutputValueSocket =
            new Dictionary<string, OutputValueSocketData>();

        public GltfInteractivityExportNode(GltfInteractivityNodeSchema schema) : base(schema)
        {
        }
        
        public override void SetSchema(GltfInteractivityNodeSchema schema, bool applySocketDescriptors,
            bool clearExistingSocketData = true)
        {
            base.SetSchema(schema, applySocketDescriptors, clearExistingSocketData);

            if (applySocketDescriptors)
            {
                if (clearExistingSocketData)
                    OutputValueSocket.Clear();
                
                foreach (var descriptor in Schema.OutputValueSockets)
                {
                    if (descriptor.Value.SupportedTypes.Length == 1 && descriptor.Value.expectedType == null)
                        OutputValueSocket.Add(descriptor.Key,
                            new OutputValueSocketData {expectedType = ExpectedType.GtlfType(descriptor.Value.SupportedTypes[0])});
                    else
                        OutputValueSocket.Add(descriptor.Key,
                            new OutputValueSocketData {expectedType = descriptor.Value.expectedType });
                }
            }
        }
    }

}