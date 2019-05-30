using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISampleModelVariant
{
	string Name { get; }
	string ModelFilePath { get; }
}

public class SampleModelVariant : ISampleModelVariant
{
	public SampleModelVariant(string modelName, string variantType, string variantName)
	{
		Name = variantType;
		ModelFilePath = $"{modelName}/{variantType}/{variantName}";
	}

	public string Name { get; }
	public string ModelFilePath { get; }
}

public class SampleModel
{
	public string Name;
	public string ScreenshotPath;
	public List<ISampleModelVariant> Variants;
	public string DefaultFilePath => Variants[0].ModelFilePath;
}

public static class SampleModelListParser
{
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
				throw new Exception("Failed to parse model name property");
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

	private static List<ISampleModelVariant> ParseVariants(JsonReader reader, string modelName)
	{
		var variants = new List<ISampleModelVariant>();

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

		return new SampleModelVariant(modelName, variantType, variantName);
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
