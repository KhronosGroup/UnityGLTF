using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// A keyframe animation.
	/// </summary>
	public class GLTFAnimation : GLTFChildOfRootProperty
	{
		/// <summary>
		/// An array of channels, each of which targets an animation's sampler at a
		/// node's property. Different channels of the same animation can't have equal
		/// targets.
		/// </summary>
		public List<AnimationChannel> Channels;

		/// <summary>
		/// An array of samplers that combines input and output accessors with an
		/// interpolation algorithm to define a keyframe graph (but not its target).
		/// </summary>
		public List<AnimationSampler> Samplers;

		public static GLTFAnimation Deserialize(GLTFRoot root, JsonReader reader)
		{
			var animation = new GLTFAnimation();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "channels":
						animation.Channels = reader.ReadList(() => AnimationChannel.Deserialize(root, reader));
						break;
					case "samplers":
						animation.Samplers = reader.ReadList(() => AnimationSampler.Deserialize(root, reader));
						break;
					default:
						animation.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return animation;
		}

		public GLTFAnimation()
		{
		}

		public GLTFAnimation(GLTFAnimation animation, GLTFRoot gltfRoot) : base(animation, gltfRoot)
		{
			Channels = new List<AnimationChannel>(animation.Channels.Count);
			foreach (AnimationChannel channel in animation.Channels)
			{
				Channels.Add(new AnimationChannel(channel, gltfRoot));
			}

			Samplers = new List<AnimationSampler>(animation.Samplers.Count);
			foreach (AnimationSampler sampler in animation.Samplers)
			{
				Samplers.Add(new AnimationSampler(sampler, gltfRoot));
			}
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			writer.WritePropertyName("channels");
			writer.WriteStartArray();
			foreach (var channel in Channels)
			{
				channel.Serialize(writer);
			}
			writer.WriteEndArray();

			writer.WritePropertyName("samplers");
			writer.WriteStartArray();
			foreach (var sampler in Samplers)
			{
				sampler.Serialize(writer);
			}
			writer.WriteEndArray();

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
