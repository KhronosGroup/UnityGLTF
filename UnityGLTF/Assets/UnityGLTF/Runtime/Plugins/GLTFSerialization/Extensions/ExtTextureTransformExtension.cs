using System;
using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class ExtTextureTransformExtension : IExtension
	{
		/// <summary>
		/// The offset of the UV coordinate origin as a percentage of the texture dimensions.
		/// </summary>
		public Vector2 Offset = new Vector2(0, 0);
		public static readonly Vector2 OFFSET_DEFAULT = new Vector2(0, 0);

		/// <summary>
		/// Rotate the UVs by this many radians counter-clockwise around the origin. This is equivalent
		/// to a similar rotation of the image clockwise.
		/// </summary>
		public double Rotation = 0.0f;
		public static readonly double ROTATION_DEFAULT = 0.0f;

		/// <summary>
		/// The scale factor applied to the components of the UV coordinates.
		/// </summary>
		public Vector2 Scale = new Vector2(1, 1);
		public static readonly Vector2 SCALE_DEFAULT = new Vector2(1, 1);

		/// <summary>
		/// Overrides the textureInfo texCoord value if this extension is supported.
		/// </summary>
		public Nullable<int> TexCoord = null;
		public static readonly int TEXCOORD_DEFAULT = 0;

		public ExtTextureTransformExtension(Vector2 offset, double rotation, Vector2 scale, Nullable<int> texCoord)
		{
			Offset = offset;
			Rotation = rotation;
			Scale = scale;
			TexCoord = texCoord;
		}

		public IExtension Clone(GLTFRoot root)
		{
			return new ExtTextureTransformExtension(Offset, Rotation, Scale, TexCoord);
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			if (Offset != OFFSET_DEFAULT)
			{
				ext.Add(new JProperty(
					ExtTextureTransformExtensionFactory.OFFSET,
					new JArray(Offset.X, Offset.Y)
				));
			}

			if (Rotation != ROTATION_DEFAULT)
			{
				ext.Add(new JProperty(
					ExtTextureTransformExtensionFactory.ROTATION,
					Rotation
				));
			}

			if (Scale != SCALE_DEFAULT)
			{
				ext.Add(new JProperty(
					ExtTextureTransformExtensionFactory.SCALE,
					new JArray(Scale.X, Scale.Y)
				));
			}

			if (TexCoord != null)
			{
				ext.Add(new JProperty(
					ExtTextureTransformExtensionFactory.TEXCOORD,
					TexCoord
				));
			}

			return new JProperty(ExtTextureTransformExtensionFactory.EXTENSION_NAME, ext);
		}
	}
}
