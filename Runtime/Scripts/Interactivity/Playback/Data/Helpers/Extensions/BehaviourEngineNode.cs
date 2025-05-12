using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    partial class BehaviourEngineNode
    {
        public bool TryGetPointer(string pointerString, out IPointer pointer)
        {
            return engine.TryGetPointer(pointerString, this, out pointer);
        }

        public bool TryGetPointer<T>(string pointerString, out Pointer<T> pointer)
        {
            return engine.TryGetPointer(pointerString, this, out pointer);
        }

        public bool TryGetReadOnlyPointer(string pointerString, out IReadOnlyPointer pointer)
        {
            return engine.TryGetPointer(pointerString, this, out pointer);
        }

        public bool TryGetReadOnlyPointer<T>(string pointerString, out ReadOnlyPointer<T> pointer)
        {
            return engine.TryGetPointer(pointerString, this, out pointer);
        }

        public bool TryGetPointerFromConfiguration(out IPointer pointer)
        {
            pointer = default;

            if (!configuration.TryGetValue(ConstStrings.POINTER, out Configuration config))
                return false;

            var pointerPath = Parser.ToString(config.value);

            return TryGetPointer(pointerPath, out pointer);
        }

        public bool TryGetPointerFromConfiguration<T>(out Pointer<T> pointer)
        {
            pointer = default;

            if (!TryGetPointerFromConfiguration(out IPointer p))
                return false;

            pointer = (Pointer<T>)p;
            return true;
        }

        public bool TryGetPointerFromConfiguration(out IReadOnlyPointer readOnlyPointer)
        {
            readOnlyPointer = default;

            if (!TryGetPointerFromConfiguration(out IPointer p))
                return false;

            readOnlyPointer = (IReadOnlyPointer)p;
            return true;
        }

        public bool TryGetPointerFromConfiguration<T>(out ReadOnlyPointer<T> readOnlyPointer)
        {
            readOnlyPointer = default;

            if (!TryGetPointerFromConfiguration(out IPointer p))
                return false;

            readOnlyPointer = (ReadOnlyPointer<T>)p;
            return true;
        }

        public bool TryEvaluateValue<T>(string valueId, out T value)
        {
            value = default;

            if (!TryEvaluateValue(valueId, out IProperty property))
                return false;

            value = ((Property<T>)property).value;
            return true;
        }

        public bool TryGetVariableFromConfiguration(out Variable variable, out int index)
        {
            variable = null;
            index = -1;

            if (!configuration.TryGetValue(ConstStrings.VARIABLE, out Configuration config))
                return false;

            try
            {
                index = Parser.ToInt(config.value);
                variable = engine.graph.variables[index];
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }
    }
}