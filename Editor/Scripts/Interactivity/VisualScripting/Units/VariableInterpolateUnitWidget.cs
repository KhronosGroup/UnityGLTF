using System;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    [Widget(typeof(VariableInterpolate))]
    public sealed class VariableInterpolateUnitWidget : UnitWidget<VariableInterpolate>
    {
        public VariableInterpolateUnitWidget(FlowCanvas canvas, VariableInterpolate unit) : base(canvas, unit)
        {
            nameInspectorConstructor = (metadata) => new VariableNameInspector(metadata, GetNameSuggestions);
        }

        protected override NodeColorMix baseColor => NodeColorMix.TealReadable;

        private VariableNameInspector nameInspector;
        private Func<Metadata, VariableNameInspector> nameInspectorConstructor;

        public override Inspector GetPortInspector(IUnitPort port, Metadata metadata)
        {
            if (port == unit.name)
            {
                // This feels so hacky. The real holy grail here would be to support attribute decorators like Unity does.
                InspectorProvider.instance.Renew(ref nameInspector, metadata, nameInspectorConstructor);

                return nameInspector;
            }

            return base.GetPortInspector(port, metadata);
        }

        private IEnumerable<string> GetNameSuggestions()
        {
            return EditorVariablesUtility.GetVariableNameSuggestions(unit.kind, reference);
        }
    }
}
