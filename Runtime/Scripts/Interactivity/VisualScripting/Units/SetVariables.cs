using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Sets several variables at once. All values are evaluated before any variable is written,
    /// like KHR_interactivity's variable/set with multiple variables.
    /// </summary>
    [UnitCategory("Variables")]
    [UnitTitle("Set Variables")]
    public class SetVariables : Unit
    {
        [SerializeAs(nameof(count))]
        private int _count = 2;

        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Count")]
        public int count
        {
            get => _count;
            set => _count = Mathf.Clamp(value, 1, 32);
        }

        /// <summary>
        /// The kind of the variables.
        /// </summary>
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VariableKind kind { get; set; } = VariableKind.Graph;

        /// <summary>
        /// The source of the variables.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        [NullMeansSelf]
        public ValueInput @object { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput assign { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlOutput assigned { get; private set; }

        [DoNotSerialize]
        public List<ValueInput> names { get; } = new List<ValueInput>();

        [DoNotSerialize]
        public List<ValueInput> values { get; } = new List<ValueInput>();

        protected override void Definition()
        {
            if (kind == VariableKind.Object)
                @object = ValueInput<GameObject>(nameof(@object), null).NullMeansSelf();

            assign = ControlInput(nameof(assign), Assign);
            assigned = ControlOutput(nameof(assigned));
            Succession(assign, assigned);

            names.Clear();
            values.Clear();
            for (int i = 0; i < count; i++)
            {
                var name = ValueInput("name_" + i, string.Empty);
                var value = ValueInput<object>("value_" + i).AllowsNull();
                Requirement(name, assign);
                Requirement(value, assign);
                names.Add(name);
                values.Add(value);
            }

            if (@object != null)
                Requirement(@object, assign);
        }

        private ControlOutput Assign(Flow flow)
        {
            var variableNames = new string[count];
            var newValues = new object[count];
            for (int i = 0; i < count; i++)
            {
                variableNames[i] = flow.GetValue<string>(names[i]);
                newValues[i] = flow.GetValue(values[i]);
            }

            var declarations = GetDeclarations(flow);
            for (int i = 0; i < count; i++)
                declarations.Set(variableNames[i], newValues[i]);

            return assigned;
        }

        private VariableDeclarations GetDeclarations(Flow flow)
        {
            switch (kind)
            {
                case VariableKind.Flow:
                    return flow.variables;
                case VariableKind.Graph:
                    return Variables.Graph(flow.stack);
                case VariableKind.Object:
                    return Variables.Object(flow.GetValue<GameObject>(@object));
                case VariableKind.Scene:
                    return Variables.Scene(flow.stack.scene);
                case VariableKind.Application:
                    return Variables.Application;
                case VariableKind.Saved:
                    return Variables.Saved;
                default:
                    throw new UnexpectedEnumValueException<VariableKind>(kind);
            }
        }
    }
}
