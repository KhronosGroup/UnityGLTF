using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class SampleModelVariant
{
	public string Type;
	public string FileName;
}

public class SampleModel
{
	public string Name;
	public string ScreenshotPath;
	public List<SampleModelVariant> Variants;
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
					result.Variants = ParseVariants(reader);
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

	private static List<SampleModelVariant> ParseVariants(JsonReader reader)
	{
		var variants = new List<SampleModelVariant>();

		ParseStartObject(reader, "variants");

		while (reader.TokenType != JsonToken.EndObject)
		{
			variants.Add(ParseVariant(reader));
		}

		ParseEndObject(reader, "variants");

		return variants;
	}

	private static SampleModelVariant ParseVariant(JsonReader reader)
	{
		var result = new SampleModelVariant();

		if (reader.TokenType != JsonToken.PropertyName)
		{
			throw new Exception("Failed to parse model variant name");
		}
		result.Type = reader.Value.ToString();
		reader.Read();

		if (reader.TokenType != JsonToken.String)
		{
			throw new Exception("Failed to parse model variant filename");
		}
		result.FileName = reader.Value.ToString();
		reader.Read();

		return result;
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
