using GLTF;
using GLTF.Schema;

namespace UnityGLTF.Cache
{
	public struct AnimationSamplerCacheData
	{
		public AttributeAccessor Input;
		public AttributeAccessor Output;
		public InterpolationType Interpolation;
	}

	public class AnimationCacheData
	{
		public UnityEngine.AnimationClip LoadedAnimationClip { get; set; }
		public AnimationSamplerCacheData[] Samplers { get; set; }

		public AnimationCacheData(int samplerCount)
		{
			Samplers = new AnimationSamplerCacheData[samplerCount];
		}
	}
}
