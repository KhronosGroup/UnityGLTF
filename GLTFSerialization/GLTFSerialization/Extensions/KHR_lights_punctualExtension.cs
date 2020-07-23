using System;
using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GLTF.Schema
{
	public class KHR_lights_punctualExtension : IExtension
	{
		/// <summary>
		/// The offset of the UV coordinate origin as a percentage of the texture dimensions.
		/// </summary>
		//public Vector2 Offset = new Vector2(0, 0);
		//public static readonly Vector2 OFFSET_DEFAULT = new Vector2(0, 0);

		public Color Color = new Color(1, 1, 1, 1);
		public static readonly Color COLOR_DEFAULT = new Color(1, 1, 1, 1);

		public string Type;
		public static readonly string TYPE_DEFAULT = new string(new char[5]{'p', 'o', 'i', 'n', 't'});

		/// <summary>
		/// The scale factor applied to the components of the UV coordinates.
		/// </summary>
		//public Vector2 Scale = new Vector2(1, 1);
		//public static readonly Vector2 SCALE_DEFAULT = new Vector2(1, 1);
		//
		///// <summary>
		///// Overrides the textureInfo texCoord value if this extension is supported.
		///// </summary>
		//public int TexCoord = 0;
		//public static readonly int TEXCOORD_DEFAULT = 0;

		List<GLTFLight> Lights;

		//public KHR_lights_punctualExtension(Color color, string type)
		//{
		//	Color = color;
		//	Type = type;
		//	//Offset = offset;
		//	//Scale = scale;
		//	//TexCoord = texCoord;
		//}

		public KHR_lights_punctualExtension(List<GLTFLight> lights)
		{
			//save to this class, then serialize lights later
			Lights = lights;
		}

		public void AddLight(GLTFLight light)
		{
			if (Lights == null)
				Lights = new List<GLTFLight>();
			Lights.Add(light);
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_lights_punctualExtension(Lights);
			//return new KHR_lights_punctualExtension(Color,Type);
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			JArray lightarray = new JArray();

			foreach (GLTFLight light in Lights)
			{
				var l = new JObject();
				l.Add(new JProperty("color", new JArray(light.color.R, light.color.G, light.color.B)));
				l.Add(new JProperty("type", light.type));

				//make this an object
				//all properties
				lightarray.Add(l);
				//ext.Add(new JProperty("type", light.type));
			}
			ext.Add("lights",lightarray);

			//should serialize all lights registered in root lights

			//if (Color != COLOR_DEFAULT)
			//{
			//	ext.Add(new JProperty(
			//		KHR_lights_punctualExtensionFactory.COLOR,
			//		new JArray(Color.R, Color.G, Color.B)
			//	));
			//}
			//
			//if (Type != TYPE_DEFAULT)
			//{
			//	ext.Add(new JProperty(
			//		KHR_lights_punctualExtensionFactory.TYPE,
			//		Type
			//	));
			//}



			return new JProperty(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, ext);
		}
	}
}
