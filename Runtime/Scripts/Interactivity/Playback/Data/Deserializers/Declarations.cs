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
            var jDeclarations = jObj[ConstStrings.DECLARATIONS].Children();
            var declarations = new List<Declaration>(jDeclarations.Count());

            foreach (var v in jDeclarations)
            {
                var declaration = new Declaration();
                declaration.op = v[ConstStrings.OP].Value<string>();

                var jExtension = v[ConstStrings.EXTENSION];

                if (jExtension != null)
                {
                    declaration.extension = jExtension.Value<string>();
                    PopulateValueSockets(v, declaration, types);
                }

                declarations.Add(declaration);
            }

            return declarations;
        }

        private static void PopulateValueSockets(JToken v, Declaration declaration, List<Type> types)
        {
            var inputs = v[ConstStrings.INPUT_VALUE_SOCKETS] as JObject;
            var outputs = v[ConstStrings.OUTPUT_VALUE_SOCKETS] as JObject;

            declaration.inputValueSockets = GetValueSockets(inputs);
            declaration.outputValueSockets = GetValueSockets(outputs);
        }

        private static List<ValueSocket> GetValueSockets(JObject jList)
        {
            if (jList == null || jList.Count <= 0)
                return null;

            var valueSockets = new List<ValueSocket>();

            foreach (var kvp in jList)
            {
                valueSockets.Add(new ValueSocket(kvp.Key, kvp.Value[ConstStrings.TYPE].Value<int>()));
            }

            return valueSockets;
        }
    }
}