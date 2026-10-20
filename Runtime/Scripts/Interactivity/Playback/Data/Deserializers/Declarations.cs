using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityGLTF.Interactivity.Playback
{
    public static class DeclarationsDeserializer
    {
        public static List<Declaration> GetDeclarations(JObject jObj, List<Type> types)
        {
            var declarations = new List<Declaration>();
            var index = 0;

            foreach (var v in GraphJson.OptionalArray(jObj, ConstStrings.DECLARATIONS, "graph"))
            {
                var context = $"declarations[{index}]";
                GraphJson.AsObject(v, context);

                var declaration = new Declaration
                {
                    op = GraphJson.RequiredString(v, ConstStrings.OP, context),
                    extension = GraphJson.OptionalString(v, ConstStrings.EXTENSION, context)
                };

                if (declaration.extension == null)
                {
                    if (!SpecOperations.IsCore(declaration.op))
                        GraphJson.Reject($"{context}: operation \"{declaration.op}\" is not defined by this specification and no extension is given.");

                    if (v[ConstStrings.INPUT_VALUE_SOCKETS] != null || v[ConstStrings.OUTPUT_VALUE_SOCKETS] != null)
                        GraphJson.Reject($"{context}: core operations must not define inputValueSockets or outputValueSockets.");
                }
                else
                {
                    declaration.inputValueSockets = GetValueSockets(GraphJson.OptionalObject(v, ConstStrings.INPUT_VALUE_SOCKETS, context), types, context);
                    declaration.outputValueSockets = GetValueSockets(GraphJson.OptionalObject(v, ConstStrings.OUTPUT_VALUE_SOCKETS, context), types, context);
                }

                for (int i = 0; i < declarations.Count; i++)
                {
                    if (AreEqual(declarations[i], declaration))
                        GraphJson.Reject($"{context}: equal to declarations[{i}].");
                }

                declarations.Add(declaration);
                index++;
            }

            return declarations;
        }

        /// <summary>
        /// Declarations are equal when op, extension, and input value sockets (ids and type indices) match.
        /// Output value sockets do not participate in equality.
        /// </summary>
        public static bool AreEqual(Declaration a, Declaration b)
        {
            if (a.op != b.op || a.extension != b.extension)
                return false;

            var aIn = a.inputValueSockets ?? new List<ValueSocket>();
            var bIn = b.inputValueSockets ?? new List<ValueSocket>();

            if (aIn.Count != bIn.Count)
                return false;

            return aIn.All(x => bIn.Any(y => y.name == x.name && y.type == x.type));
        }

        private static List<ValueSocket> GetValueSockets(JObject jList, List<Type> types, string context)
        {
            if (jList == null || jList.Count <= 0)
                return null;

            var valueSockets = new List<ValueSocket>();

            foreach (var kvp in jList)
            {
                var socketContext = $"{context} socket \"{kvp.Key}\"";
                GraphJson.AsObject(kvp.Value, socketContext);
                valueSockets.Add(new ValueSocket(kvp.Key, GraphJson.RequiredIndex(kvp.Value, ConstStrings.TYPE, types.Count, socketContext)));
            }

            return valueSockets;
        }
    }
}
