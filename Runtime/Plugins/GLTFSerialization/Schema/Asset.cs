using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	/// <summary>
	/// Metadata about the glTF asset.
	/// </summary>
	public class Asset : GLTFProperty
	{
		/// <summary>
		/// A copyright message suitable for display to credit the content creator.
		/// </summary>
		public string Copyright;

		/// <summary>
		/// Tool that generated this glTF model. Useful for debugging.
		/// </summary>
		public string Generator;

		/// <summary>
		/// The glTF version.
		/// </summary>
		public string Version;

		/// <summary>
		/// The minimum glTF version that this asset targets.
		/// </summary>
		public string MinVersion;
		
		public Dictionary<string, JToken> PluginExtras = new Dictionary<string, JToken>();

		public Asset()
		{
		}

		public Asset(Asset asset) : base(asset)
		{
			if (asset == null) return;

			Copyright = asset.Copyright;
			Generator = asset.Generator;
			Version = asset.Version;
			MinVersion = asset.MinVersion;
		}

		public static Asset Deserialize(GLTFRoot root, JsonReader reader)
		{
			var asset = new Asset();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Asset must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "copyright":
						asset.Copyright = reader.ReadAsString();
						break;
					case "generator":
						asset.Generator = reader.ReadAsString();
						break;
					case "version":
						asset.Version = reader.ReadAsString();
						break;
					case "minVersion":
						asset.MinVersion = reader.ReadAsString();
						break;
					default:
						asset.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return asset;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Copyright != null)
			{
				writer.WritePropertyName("copyright");
				writer.WriteValue(Copyright);
			}

			if (Generator != null)
			{
				writer.WritePropertyName("generator");
				writer.WriteValue(Generator);
			}

			writer.WritePropertyName("version");
			writer.WriteValue(Version);

			if (PluginExtras.Count > 0)
			{
				writer.WritePropertyName("extras");
				writer.WriteStartObject();
				writer.WritePropertyName("plugins");
				foreach (var extra in PluginExtras)
				{
					writer.WriteStartObject();
					writer.WritePropertyName(extra.Key);
					extra.Value.WriteTo(writer);
					writer.WriteEndObject();
				}
				writer.WriteEndObject();
			}
			
			base.Serialize(writer);

			writer.WriteEndObject();
		}

		public override string ToString()
		{
			return ToString(false);
		}
		
		public string ToString(bool richFormat)
		{
			string bStart = richFormat ? "<b>" : "";
			string bEnd = richFormat ? "</b>" : "";
			
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(Generator))
				sb.AppendLine($"{bStart}{nameof(Generator)}: {bEnd}{Generator}");
			
			if (!string.IsNullOrEmpty(Version))
				sb.AppendLine($"{bStart}{nameof(Version)}: {bEnd}{Version}");
			
			if (!string.IsNullOrEmpty(MinVersion))
				sb.AppendLine($"{bStart}{nameof(MinVersion)}: {bEnd}{MinVersion}");
			
			if (!string.IsNullOrEmpty(Copyright))
				sb.AppendLine($"{bStart}{nameof(Copyright)}: {bEnd}{Copyright}");
			    
			if (PluginExtras != null)
			{
				sb.AppendLine("");
				sb.AppendLine($"{bStart}Extras: {bEnd}");
				foreach (var extra in PluginExtras)
					sb.AppendLine(extra.ToString());
			}

			return sb.ToString();
		}
	}
}
