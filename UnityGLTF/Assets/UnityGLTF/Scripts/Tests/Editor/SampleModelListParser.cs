using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF
{
	public class SampleModelVariant
	{
		public SampleModelVariant(string name, string modelFilePath)
		{
			Name = name;
			ModelFilePath = modelFilePath;
		}

		public string Name { get; }
		public string ModelFilePath { get; }
	}

	public class SampleModel
	{
		public string Name;
		public string ScreenshotPath;
		public List<SampleModelVariant> Variants;
		public string DefaultFilePath => Variants[0].ModelFilePath;
		public bool Expanded = false;
	}

	public static class SampleModelListParser
	{
		public enum ListType
		{
			SampleModels,
			AssetGenerator
		}

		public static ListType DetermineListSource(JsonReader reader)
		{
			ParseStartArray(reader, "models");
			ParseStartObject(reader, "modelOrFolder");

			if (reader.TokenType != JsonToken.PropertyName)
			{
				throw new Exception("Failed to parse first property to determine list type");
			}

			string firstProperty = reader.Value.ToString();

			switch (firstProperty.ToLowerInvariant())
			{
				case "folder":
				case "id":
				case "models":
					return ListType.AssetGenerator;

				case "name":
				case "screenshot":
				case "variants":
					return ListType.SampleModels;

				default:
					throw new FormatException("Error parsing json to determine list type");
			}
		}

		public static List<SampleModel> ParseAssetGeneratorModels(JsonReader reader)
		{
			var models = new List<SampleModel>();

			ParseStartArray(reader, "folders");

			while (reader.TokenType != JsonToken.EndArray)
			{
				ParseAssetGeneratorFolder(reader, models);
			}

			ParseEndArray(reader, "folders");

			return models;
		}

		private static void ParseAssetGeneratorFolder(JsonReader reader, IList<SampleModel> models)
		{
			string folderName = null;

			ParseStartObject(reader, "folder");

			while (reader.TokenType != JsonToken.EndObject)
			{
				if (reader.TokenType != JsonToken.PropertyName)
				{
					throw new Exception("Failed to parse folder property");
				}
				var propertyName = reader.Value.ToString().ToLowerInvariant();
				reader.Read();

				switch (propertyName)
				{
					case "folder":
						folderName = ParsePropertyValueAsString(reader, propertyName);
						break;
					case "id":
						// There will be a number.  Read that and ignore it.
						reader.Read();
						break;
					case "models":
						Debug.Assert(!string.IsNullOrEmpty(folderName), "Asset generator list should have folder listed before models.");
						ParseAssetGeneratorModels(reader, models, folderName);
						break;
				}
			}

			ParseEndObject(reader, "folder");
		}

		private static void ParseAssetGeneratorModels(JsonReader reader, IList<SampleModel> models, string folderName)
		{
			ParseStartArray(reader, "models");

			while (reader.TokenType != JsonToken.EndArray)
			{
				models.Add(ParseAssetGeneratorModel(reader, folderName));
			}

			ParseEndArray(reader, "models");
		}

		private static SampleModel ParseAssetGeneratorModel(JsonReader reader, string folderName)
		{
			var result = new SampleModel();
			result.Variants = new List<SampleModelVariant>();

			ParseStartObject(reader, "model");

			while (reader.TokenType != JsonToken.EndObject)
			{
				if (reader.TokenType != JsonToken.PropertyName)
				{
					throw new Exception("Failed to parse model property");
				}
				var propertyName = reader.Value.ToString().ToLowerInvariant();
				reader.Read();

				switch (propertyName)
				{
					case "filename":
						var fileName = ParsePropertyValueAsString(reader, propertyName);
						result.Name = fileName;
						result.Variants.Add(new SampleModelVariant(result.Name, $"{folderName}/{fileName}"));
						break;
					case "loadable":
						// There will be a bool.  Read that and ignore it.
						reader.Read();
						break;
					case "sampleimagename":
						result.ScreenshotPath = ParsePropertyValueAsString(reader, propertyName);
						break;
					case "camera":
						ParseCamera(reader);
						break;
				}
			}

			ParseEndObject(reader, "model");

			return result;
		}

		private static void ParseCamera(JsonReader reader)
		{
			ParseStartObject(reader, "camera");

			ParseToken(reader, JsonToken.PropertyName, "translation");
			ParseStartArray(reader, "translation");
			while (reader.TokenType != JsonToken.EndArray)
			{
				reader.Read();
			}
			ParseEndArray(reader, "translation");

			ParseEndObject(reader, "camera");
		}

		private static void ParseToken(JsonReader reader, JsonToken expectedTokenType, string expectedString)
		{
			if (reader.TokenType != expectedTokenType
				|| reader.Value.ToString().ToLowerInvariant() != expectedString)
			{
				throw new Exception($"Failed to parse {expectedString}");
			}
			reader.Read();
		}

		public static List<SampleModel> ParseSampleModels(JsonReader reader)
		{
			var models = new List<SampleModel>();

			ParseStartArray(reader, "models");

			while (reader.TokenType != JsonToken.EndArray)
			{
				models.Add(ParseSampleModel(reader));
			}

			ParseEndArray(reader, "models");

			return models;
		}

		private static SampleModel ParseSampleModel(JsonReader reader)
		{
			var result = new SampleModel();

			ParseStartObject(reader, "model");

			while (reader.TokenType != JsonToken.EndObject)
			{
				if (reader.TokenType != JsonToken.PropertyName)
				{
					throw new Exception("Failed to parse model property");
				}
				var propertyName = reader.Value.ToString().ToLowerInvariant();
				reader.Read();

				switch (propertyName)
				{
					case "name":
						result.Name = ParsePropertyValueAsString(reader, propertyName);
						break;
					case "screenshot":
						result.ScreenshotPath = ParsePropertyValueAsString(reader, propertyName);
						break;
					case "variants":
						Debug.Assert(!string.IsNullOrEmpty(result.Name), "Model index should have a name before a list of variants.");
						result.Variants = ParseVariants(reader, result.Name);
						break;
				}
			}

			ParseEndObject(reader, "model");

			return result;
		}

		private static string ParsePropertyValueAsString(JsonReader reader, string propertyName)
		{
			if (reader.TokenType != JsonToken.String)
			{
				throw new Exception($"Failed to parse string value for {propertyName}");
			}
			var result = reader.Value.ToString();

			reader.Read();

			return result;
		}

		private static List<SampleModelVariant> ParseVariants(JsonReader reader, string modelName)
		{
			var variants = new List<SampleModelVariant>();

			ParseStartObject(reader, "variants");

			while (reader.TokenType != JsonToken.EndObject)
			{
				variants.Add(ParseVariant(reader, modelName));
			}

			ParseEndObject(reader, "variants");

			return variants;
		}

		private static SampleModelVariant ParseVariant(JsonReader reader, string modelName)
		{
			if (reader.TokenType != JsonToken.PropertyName)
			{
				throw new Exception("Failed to parse model variant name");
			}
			string variantType = reader.Value.ToString();
			reader.Read();

			if (reader.TokenType != JsonToken.String)
			{
				throw new Exception("Failed to parse model variant filename");
			}
			string variantName = reader.Value.ToString();
			reader.Read();

			return new SampleModelVariant(variantType, $"{modelName}/{variantType}/{variantName}");
		}

		private static void ParseStartObject(JsonReader reader, string objectName)
		{
			if (reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception($"Failed to parse {objectName} start");
			}
			reader.Read();
		}

		private static void ParseEndObject(JsonReader reader, string objectName)
		{
			if (reader.TokenType != JsonToken.EndObject)
			{
				throw new Exception($"Failed to parse {objectName} end");
			}
			reader.Read();
		}

		private static void ParseStartArray(JsonReader reader, string objectName)
		{
			if (reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception($"Failed to parse {objectName} start");
			}
			reader.Read();
		}

		private static void ParseEndArray(JsonReader reader, string objectName)
		{
			if (reader.TokenType != JsonToken.EndArray)
			{
				throw new Exception($"Failed to parse {objectName} end");
			}
			reader.Read();
		}
	}
}
