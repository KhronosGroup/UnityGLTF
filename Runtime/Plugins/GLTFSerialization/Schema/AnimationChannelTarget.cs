using System;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// The index of the node and TRS property that an animation channel targets.
	/// </summary>
	public class AnimationChannelTarget : GLTFProperty
	{
		/// <summary>
		/// The index of the node to target.
		/// </summary>
		public NodeId Node;

		/// <summary>
		/// The name of the node's TRS property to modify.
		/// </summary>
		public string Path;

		public static AnimationChannelTarget Deserialize(GLTFRoot root, JsonReader reader)
		{
			var animationChannelTarget = new AnimationChannelTarget();

			if (reader.Read() && reader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("Animation channel target must be an object.");
			}

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "node":
						animationChannelTarget.Node = NodeId.Deserialize(root, reader);
						break;
					case "path":
						animationChannelTarget.Path = reader.ReadAsString();// reader.ReadStringEnum<GLTFAnimationChannelPath>();
						break;
					// TODO: add KHR_animation_pointer import
					// case "pointer":
					// 	break;
					default:
						animationChannelTarget.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return animationChannelTarget;
		}

		public AnimationChannelTarget()
		{
		}

		public AnimationChannelTarget(AnimationChannelTarget channelTarget, GLTFRoot gltfRoot) : base(channelTarget)
		{
			if (channelTarget == null) return;

			Node = channelTarget.Node != null ? new NodeId(channelTarget.Node, gltfRoot) : null;
			Path = channelTarget.Path;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			// in KHR_animation2 node might not exist, instead it has an extensions field
			if (Node != null)
			{
				writer.WritePropertyName("node");
				writer.WriteValue(Node.Id);
			}

			writer.WritePropertyName("path");
			writer.WriteValue(Path.ToString());

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}

	public enum GLTFAnimationChannelPath
	{
		translation,
		rotation,
		scale,
		weights,
		pointer
	}
}
