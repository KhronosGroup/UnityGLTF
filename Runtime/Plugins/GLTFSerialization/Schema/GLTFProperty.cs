using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Schema.KHR_lights_punctual;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class GLTFProperty
	{
		public static IReadOnlyList<string> RegisteredExtensions
		{
			get
			{
				lock (_extensionRegistry)
				{
					return _extensionRegistry.Keys.ToList();
				}
			}
		}
		
		private static Dictionary<string, ExtensionFactory> _extensionRegistry = new Dictionary<string, ExtensionFactory>()
		{
			{ ExtTextureTransformExtensionFactory.EXTENSION_NAME, new ExtTextureTransformExtensionFactory() },
			{ KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME, new KHR_materials_pbrSpecularGlossinessExtensionFactory() },
			{ KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME, new KHR_MaterialsUnlitExtensionFactory() },
			{ KHR_lights_punctualExtensionFactory.EXTENSION_NAME, new KHR_lights_punctualExtensionFactory() },
			{ KHR_materials_emissive_strength_Factory.EXTENSION_NAME, new KHR_materials_emissive_strength_Factory() },
			{ KHR_materials_transmission_Factory.EXTENSION_NAME, new KHR_materials_transmission_Factory() },
			{ KHR_materials_volume_Factory.EXTENSION_NAME, new KHR_materials_volume_Factory() },
			{ KHR_materials_ior_Factory.EXTENSION_NAME, new KHR_materials_ior_Factory() },
			{ KHR_materials_iridescence_Factory.EXTENSION_NAME, new KHR_materials_iridescence_Factory() },
			{ KHR_materials_specular_Factory.EXTENSION_NAME, new KHR_materials_specular_Factory() },
			{ KHR_materials_clearcoat_Factory.EXTENSION_NAME, new KHR_materials_clearcoat_Factory() },
			{ MSFT_LODExtensionFactory.EXTENSION_NAME, new MSFT_LODExtensionFactory() },
			{ KHR_draco_mesh_compression_Factory.EXTENSION_NAME, new KHR_draco_mesh_compression_Factory() },
			{ KHR_texture_basisu_Factory.EXTENSION_NAME, new KHR_texture_basisu_Factory() },
			{ EXT_meshopt_compression_Factory.EXTENSION_NAME, new EXT_meshopt_compression_Factory() },
			{ EXT_mesh_gpu_instancing_Factory.EXTENSION_NAME, new EXT_mesh_gpu_instancing_Factory() },
			{ KHR_animation_pointerExtensionFactory.EXTENSION_NAME, new KHR_animation_pointerExtensionFactory() },
			{ KHR_materials_dispersion_Factory.EXTENSION_NAME, new KHR_materials_dispersion_Factory() },
			{ KHR_materials_sheen_Factory.EXTENSION_NAME, new KHR_materials_sheen_Factory()},
			{ KHR_node_visibility_Factory.EXTENSION_NAME, new KHR_node_visibility_Factory()},
			{ KHR_node_selectability_Factory.EXTENSION_NAME, new KHR_node_selectability_Factory()},
			{ KHR_node_hoverability_Factory.EXTENSION_NAME, new KHR_node_hoverability_Factory()},
			{ KHR_audio_emitterFactory.EXTENSION_NAME, new KHR_audio_emitterFactory()},
			{ KHR_materials_anisotropy_Factory.EXTENSION_NAME, new KHR_materials_anisotropy_Factory()},
		};

		private static DefaultExtensionFactory _defaultExtensionFactory = new DefaultExtensionFactory();

		public static bool IsExtensionRegistered(string extensionName)
		{
			lock (_extensionRegistry)
			{
				return _extensionRegistry.ContainsKey(extensionName);
			}
		}

		public static void RegisterExtension(ExtensionFactory extensionFactory)
		{
			lock (_extensionRegistry)
			{
				_extensionRegistry[extensionFactory.ExtensionName] = extensionFactory;
			}
		}

		public static ExtensionFactory TryGetExtension(string extensionName)
		{
			lock (_extensionRegistry)
			{
				ExtensionFactory result;
				if (_extensionRegistry.TryGetValue(extensionName, out result))
				{
					return result;
				}
				return null;
			}
		}

		public static IExtension CreateEmptyExtension(string extensionName)
		{
			return TryGetExtension(extensionName)?.Deserialize(null, new JProperty(extensionName, new JObject()));
		}

		public static bool TryRegisterExtension(ExtensionFactory extensionFactory)
		{
			lock (_extensionRegistry)
			{
				if (_extensionRegistry.ContainsKey(extensionFactory.ExtensionName))
				{
					return false;
				}
				_extensionRegistry.Add(extensionFactory.ExtensionName, extensionFactory);
				return true;
			}
		}

		public Dictionary<string, IExtension> Extensions;
		public JToken Extras;

		public GLTFProperty()
		{
		}

		public GLTFProperty(GLTFProperty property, GLTFRoot gltfRoot = null)
		{
			if (property == null) return;

			if (property.Extensions != null)
			{
				Extensions = new Dictionary<string, IExtension>(property.Extensions.Count);
				foreach (KeyValuePair<string, IExtension> extensionKeyValuePair in property.Extensions)
				{
					Extensions.Add(extensionKeyValuePair.Key, extensionKeyValuePair.Value.Clone(gltfRoot));
				}
			}

			if (property.Extras != null)
			{
				Extras = property.Extras.DeepClone();
			}
		}

		public void DefaultPropertyDeserializer(GLTFRoot root, JsonReader reader)
		{
			switch (reader.Value.ToString())
			{
				case "extensions":
					Extensions = DeserializeExtensions(root, reader);
					break;
				case "extras":
					// advance to property value
					reader.Read();
					if (reader.TokenType != JsonToken.StartObject)
						throw new Exception(string.Format("extras must be an object at: {0}", reader.Path));
					Extras = JToken.ReadFrom(reader);
					break;
				default:
					SkipValue(reader);
					break;
			}
		}

		public void AddExtension(string extname, IExtension extension)
		{
			if (Extensions == null)
			{
				Extensions = new Dictionary<string, IExtension>();
			}
			if (Extensions.ContainsKey(extname))
			{
				throw new Exception($"Extension {extname} already exist on GLTFProperty");
			}
			Extensions.Add(extname, extension);
		}

		private void SkipValue(JsonReader reader)
		{
			if (!reader.Read())
			{
				throw new Exception("No value found.");
			}

			if (reader.TokenType == JsonToken.StartObject)
			{
				SkipObject(reader);
			}
			else if (reader.TokenType == JsonToken.StartArray)
			{
				SkipArray(reader);
			}
		}

		private void SkipObject(JsonReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndObject)
			{
				if (reader.TokenType == JsonToken.StartArray)
				{
					SkipArray(reader);
				}
				else if (reader.TokenType == JsonToken.StartObject)
				{
					SkipObject(reader);
				}
			}
		}

		private void SkipArray(JsonReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				if (reader.TokenType == JsonToken.StartArray)
				{
					SkipArray(reader);
				}
				else if (reader.TokenType == JsonToken.StartObject)
				{
					SkipObject(reader);
				}
			}
		}

		internal static Dictionary<string, IExtension> DeserializeExtensions(GLTFRoot root, JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new GLTFParseException("GLTF extensions must be an object");
			}

			JObject extensions = (JObject)JToken.ReadFrom(reader);
			var extensionsCollection = new Dictionary<string, IExtension>();

			foreach(JToken child in extensions.Children())
			{
				if (child.Type != JTokenType.Property)
				{
					throw new GLTFParseException("Children token of extensions should be properties");
				}

				JProperty childAsJProperty = (JProperty) child;
				string extensionName = childAsJProperty.Name;
				ExtensionFactory extensionFactory;

				lock (_extensionRegistry)
				{
					if (_extensionRegistry.TryGetValue(extensionName, out extensionFactory))
					{
						extensionsCollection.Add(extensionName, extensionFactory.Deserialize(root, childAsJProperty));
					}
					else
					{
						extensionsCollection.Add(extensionName, _defaultExtensionFactory.Deserialize(root, childAsJProperty));
					}
				}

			}

			return extensionsCollection;
		}

		public virtual void Serialize(JsonWriter writer)
		{
			if (Extensions != null && Extensions.Count > 0)
			{
				writer.WritePropertyName("extensions");
				writer.WriteStartObject();
				foreach (var extension in Extensions)
				{
					JToken extensionToken = extension.Value.Serialize();
					extensionToken.WriteTo(writer);
				}
				writer.WriteEndObject();
			}

			if(Extras != null)
			{
				writer.WritePropertyName("extras");
				Extras.WriteTo(writer);
			}
		}
	}
}
