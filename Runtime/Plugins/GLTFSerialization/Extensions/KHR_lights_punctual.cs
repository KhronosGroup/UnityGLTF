using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Utilities;
using UnityEngine;
using UnityGLTF.Extensions;
using Color = GLTF.Math.Color;

namespace GLTF.Schema.KHR_lights_punctual
{
	/// <summary>
	/// Specifies the light type.
	/// </summary>
	public enum LightType
	{
		directional,
		point,
		spot
	}

	/// <summary>
	/// Texture sampler properties for filtering and wrapping modes.
	/// </summary>
	public class Spot
	{
		private const string PNAME_INNERCONEANGLE = "innerConeAngle";
		private const string PNAME_OUTERCONEANGLE = "outerConeAngle";

		public static readonly double INNER_DEFAULT = 0d;
		public static readonly double OUTER_DEFAULT = 0.7853981633974483d;
		/// <summary>
		/// Angle, in radians, from centre of spotlight where falloff begins. Must be greater than or equal to 0 and less than outerConeAngle.
		/// </summary>
		public double InnerConeAngle = INNER_DEFAULT;

		/// <summary>
		/// Angle, in radians, from centre of spotlight where falloff ends. Must be greater than innerConeAngle and less than or equal to PI / 2.0.
		/// </summary>
		public double OuterConeAngle = OUTER_DEFAULT;

		public Spot()
		{
		}

		public static Spot Deserialize(JsonReader reader)
		{
			var spot = new Spot();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				return spot;
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case PNAME_INNERCONEANGLE:
						spot.InnerConeAngle= reader.ReadAsDouble().Value;
						break;
					case PNAME_OUTERCONEANGLE:
						spot.OuterConeAngle= reader.ReadAsDouble().Value;
						break;
				}
			}

			return spot;
		}

		public void Serialize(JsonWriter writer)
		{
			if( InnerConeAngle > OuterConeAngle)
			{
				throw new Exception("Spot's InnerConeAngle must be less or equal OuterConeAngle");
			}

			if (OuterConeAngle > 1.5707963267948966d )
			{
				throw new Exception("Spot's OuterConeAngle must be less or equal Pi/2");
			}

			writer.WriteStartObject();

			if (InnerConeAngle != INNER_DEFAULT)
			{
				writer.WritePropertyName(PNAME_INNERCONEANGLE);
				writer.WriteValue(InnerConeAngle);
			}

			if (OuterConeAngle != OUTER_DEFAULT)
			{
				writer.WritePropertyName(PNAME_OUTERCONEANGLE);
				writer.WriteValue(OuterConeAngle);
			}

			writer.WriteEndObject();
		}
	}

	public class PunctualLight : GLTFChildOfRootProperty
	{
		private const string PNAME_TYPE = "type";
		private const string PNAME_COLOR = "color";
		private const string PNAME_INTENSITY = "intensity";
		private const string PNAME_RANGE = "range";
		private const string PNAME_SPOT = "spot";

		public static readonly Color COLOR_DEFAULT = Color.White;
		public static readonly double RANGE_DEFAULT = -1d;
		public static readonly double INTENSITY_DEFAULT = 1d;

		/// <summary>
		/// Specifies the light type.
		/// </summary>
		public LightType Type;

		/// <summary>
		/// Color of the light source.
		/// </summary>
		public Color Color = COLOR_DEFAULT;

		/// <summary>
		/// Intensity of the light source. `point` and `spot` lights use luminous intensity in candela (lm/sr) while `directional` lights use illuminance in lux (lm/m^2)
		/// </summary>
		public double Intensity = INTENSITY_DEFAULT;

		/// <summary>
		/// A distance cutoff at which the light's intensity may be considered to have reached zero.
		/// </summary>
		public double Range = RANGE_DEFAULT;

		/// <summary>
		/// spot's inner and outer angle, must exist for spot types
		/// </summary>
		public Spot Spot;

		public PunctualLight()
		{

		}

		public PunctualLight(PunctualLight light, GLTFRoot gltfRoot) : base(light, gltfRoot)
		{
			Type = light.Type;
			Color = light.Color;
			Intensity = light.Intensity;
			Range = light.Range;
			Spot = light.Spot;
		}

		public static PunctualLight Deserialize(GLTFRoot root, JsonReader reader)
		{
			var light = new PunctualLight();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Light must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case PNAME_TYPE:
						light.Type = reader.ReadStringEnum<LightType>();
						break;
					case PNAME_COLOR:
						light.Color = reader.ReadAsRGBColor();
						break;
					case PNAME_INTENSITY:
						light.Intensity = reader.ReadAsDouble().Value;
						break;
					case PNAME_RANGE:
						light.Range = reader.ReadAsDouble().Value;
						break;
					case PNAME_SPOT:
						light.Spot = Spot.Deserialize(reader);
						break;
					default:
						light.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return light;
		}

		public static PunctualLight Deserialize(GLTFRoot root, JToken token)
		{
			using (JsonReader reader = new JTokenReader(token))
			{
				return Deserialize(root, reader);
			}
		}

		override public void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			writer.WritePropertyName(PNAME_TYPE);
			writer.WriteValue(Type.ToString());


			if (Color != COLOR_DEFAULT)
			{
				writer.WritePropertyName(PNAME_COLOR);
				writer.WriteStartArray();
				writer.WriteValue(Color.R);
				writer.WriteValue(Color.G);
				writer.WriteValue(Color.B);
				writer.WriteEndArray();
			}

			if (Intensity != INTENSITY_DEFAULT)
			{
				writer.WritePropertyName(PNAME_INTENSITY);
				writer.WriteValue(Intensity);
			}

			if (Range != RANGE_DEFAULT)
			{
				writer.WritePropertyName(PNAME_RANGE);
				writer.WriteValue(Range);
			}

			if (Type == LightType.spot)
			{
				writer.WritePropertyName(PNAME_SPOT);
				Spot.Serialize(writer);
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}

		public JObject Serialize()
		{
			JTokenWriter writer = new JTokenWriter();
			Serialize(writer);
			return (JObject)writer.Token;
		}
	}

	public class KHR_LightsPunctualRootExtension : IExtension
	{
		public const string EXTENSION_NAME = "KHR_lights_punctual";

		public readonly IList<GLTFLight> lights;

		public KHR_LightsPunctualRootExtension(IList<GLTFLight> lights)
		{
			this.lights = lights;
		}

		public JProperty Serialize()
		{
			var obj = new JObject();
			var arr = new JArray();
			obj.Add("lights", arr);
			foreach (var light in lights)
			{
				var lightInfo = new JObject();
				arr.Add(lightInfo);
				lightInfo.Add("type", light.type);
				if (light.range > 0) lightInfo.Add("range", light.range);
				if(System.Math.Abs(light.intensity - 1f) > .0000001) lightInfo.Add("intensity", light.intensity);
				lightInfo.Add("name",  light.Name ?? light.name);
				lightInfo.Add("color", new JArray(light.color.R, light.color.G, light.color.B));
				// TODO why is this not using the specific spotlight serializer/deserializer?
				if (light is GLTFSpotLight spotLight)
				{
					lightInfo.Add("spot", new JObject(
						new JProperty(nameof(spotLight.innerConeAngle), spotLight.innerConeAngle),
						new JProperty(nameof(spotLight.outerConeAngle), spotLight.outerConeAngle)));
				}
			}
			return new JProperty(EXTENSION_NAME, obj);
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_LightsPunctualRootExtension(lights);
		}
	}

	public class KHR_LightsPunctualExtension : IExtension, IImportAnimationPointerRootExtension
	{
		public List<PunctualLight> Lights;

		public KHR_LightsPunctualExtension()
		{
			Lights = new List<PunctualLight>();
		}

		public IExtension Clone(GLTFRoot root)
		{
			var clone = new KHR_LightsPunctualExtension();
			for (int i = 0; i < Lights.Count; i++)
			{
				clone.Lights.Add(new PunctualLight(Lights[i], root));
			}
			return clone;
		}

		public JProperty Serialize()
		{
			return new JProperty(KHR_lights_punctualExtensionFactory.EXTENSION_NAME,
				new JObject(
					new JProperty(KHR_lights_punctualExtensionFactory.PNAME_LIGHTS, new JArray(Lights))
				)
			);
		}
		
		private static string[] GltfLightPropertyToUnityPropertyName(string gltfPropertyName)
		{
			switch (gltfPropertyName)
			{
				case "color":
					return new string[] { "m_Color.r", "m_Color.g", "m_Color.b"};
				case "intensity":
					return new string[] { "m_Intensity"};
				case "range":
					return new string[]  {"m_Range"};
				case "spot/innerConeAngle":
					return new string[]  {"m_InnerConeAngle"};
				case "spot/outerConeAngle":
					return new string[]  {"m_OuterConeAngle"};
				default:
					return new string[] {gltfPropertyName};
			}
		}

		private static AnimationPointerData.ImportValuesConversion GetConversion(string gltfPropertyName)
		{
			switch (gltfPropertyName)
			{
				case "color":
					return (data, index) =>
					{
						var col = data.primaryData.AccessorContent.AsFloat3s[index];
						var color = new Color(col[0], col[1], col[2], 1f).ToUnityColorRaw();
						return new float[] { color.r, color.g, color.b };
						//return new float[] { col.x, col.y, col.z };
					};
				case "intensity":
					return (data, index) => new float[1] { data.primaryData.AccessorContent.AsFloats[index] / Mathf.PI }; 
				case "range":
					return (data, index) => new float[1] { data.primaryData.AccessorContent.AsFloats[index] };
				case "spot/innerConeAngle":
					return (data, index) => new float[] { data.primaryData.AccessorContent.AsFloats[index] * 2 / (Mathf.Deg2Rad * 0.8f)}; 
				case "spot/outerConeAngle":
					return (data, index) => new float[] { data.primaryData.AccessorContent.AsFloats[index] * 2 / Mathf.Deg2Rad};
				default:
					return null;
			}
		}

		public bool TryGetImportAnimationPointerData(GLTFRoot root, PointerPath pointerPath, out AnimationPointerData pointerData)
		{
			pointerData = new AnimationPointerData();
			pointerData.targetNodeIds = new int[0];
			pointerData.targetType = typeof(UnityEngine.Light);
			
			if (root.Nodes == null)
				return false;
			
			var pointId = pointerPath.FindNext(PointerPath.PathElement.Index);
			if (pointId == null)
				return false;

			var property = pointerPath.FindNext(PointerPath.PathElement.Property);
			if (property == null)
				return false;
			
			pointerData.unityPropertyNames = GltfLightPropertyToUnityPropertyName(property.elementName);
			pointerData.importAccessorContentConversion = GetConversion(property.elementName);

			List<int> targetNodes = new List<int>();
			for (int i = 0; i < root.Nodes.Count; i++)
			{
				var n = root.Nodes[i];
				if (n.Extensions != null && n.Extensions.TryGetValue(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, out IExtension extension))
				{
					if (!(extension is KHR_LightsPunctualNodeExtension lightExtension))
						continue;

					if (lightExtension.LightId.Id == pointId.index)
					{
						targetNodes.Add(i);
					}
				}
			}

			if (targetNodes.Count > 0)
			{
				pointerData.targetNodeIds = targetNodes.ToArray();
				return true;
			}

			return false;
		}
	}

	public class PunctualLightId : GLTFId<PunctualLight>
	{
		public PunctualLightId()
		{
		}

		public PunctualLightId(PunctualLightId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override PunctualLight Value
		{
			get
			{
				if (Root.Extensions.TryGetValue(KHR_lights_punctualExtensionFactory.EXTENSION_NAME, out IExtension iextension))
				{
					KHR_LightsPunctualExtension extension = iextension as KHR_LightsPunctualExtension;
					return extension.Lights[Id];
				}
				else
				{
					throw new Exception("KHR_lights_punctual not found on root object");
				}
			}
		}

		public static PunctualLightId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new PunctualLightId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class KHR_LightsPunctualNodeExtension : IExtension
	{
		public PunctualLightId LightId;

		public KHR_LightsPunctualNodeExtension()
		{

		}

		public KHR_LightsPunctualNodeExtension(PunctualLightId lightId, GLTFRoot gltfRoot)
		{
			LightId = lightId;
		}

		public KHR_LightsPunctualNodeExtension( int lightId, GLTFRoot gltfRoot)
		{
			LightId = new PunctualLightId
			{
				Id = lightId,
				Root = gltfRoot
			};
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new KHR_LightsPunctualNodeExtension(LightId.Id, root);
		}

		public JProperty Serialize()
		{
			return new JProperty(KHR_lights_punctualExtensionFactory.EXTENSION_NAME,
				new JObject(
					new JProperty(KHR_lights_punctualExtensionFactory.PNAME_LIGHT, LightId.Id )
				)
			);
		}
	}
}
