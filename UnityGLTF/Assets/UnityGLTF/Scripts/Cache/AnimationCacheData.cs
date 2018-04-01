using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLTF;

namespace UnityGLTF.Cache
{
	public struct AnimationSamplerCacheData
	{
		public AttributeAccessor Input;
		public AttributeAccessor Output;
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
