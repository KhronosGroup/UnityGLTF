using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    public class GLTFProperty
    {
        public Dictionary<string, object> Extensions;
        public Dictionary<string, object> Extras;

	    public void DefaultPropertyDeserializer(GLTFRoot root, JsonTextReader reader)
	    {
		    switch (reader.Value.ToString())
		    {
				case "extensions":
				    Extensions = reader.ReadAsObjectDictionary();
				    break;
			    case "extras":
				    Extras = reader.ReadAsObjectDictionary();
				    break;
			    default:
				    throw new Exception(string.Format("Unexpected property: {0} at: {1}", reader.Value, reader.Path));
			}
	    }
    }
}
