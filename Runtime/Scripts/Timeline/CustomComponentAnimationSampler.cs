#nullable enable
using UnityEngine;

namespace UnityGLTF.Timeline
{
    public interface CustomComponentAnimationSampler
    {
        public string PropertyName { get; }
        
        internal Component? GetTarget(Transform transform);
        internal object? GetValue(Transform transform, Component target);
    }
    
    /// <summary>
    ///  Allows to include data in custom components to be animated in the exported GLTF file.
    /// </summary>
    /// <typeparam name="TComponent">Type of the component the data is read from</typeparam>
    /// <typeparam name="TData">Data type. Should be Nullable if the animation may not have a value at any point in time</typeparam>
    public interface CustomComponentAnimationSampler<TComponent, TData> : CustomComponentAnimationSampler
        where TComponent : UnityEngine.Component
    {
        Component? CustomComponentAnimationSampler.GetTarget(Transform transform) => getTarget(transform);

        object? CustomComponentAnimationSampler.GetValue(Transform transform, Component target) => 
            target is TComponent tcomp ? getValue(transform, tcomp) : null;

        /// While sampling, gets the target of the animation from a transform
        /// <param name="transform"></param>
        /// <returns>get the target of the animation, or null if it is not found</returns>
        protected TComponent? getTarget(Transform transform);
        
        /// <summary>
        /// get the value the animation is changing., or null if it is not present
        /// </summary>
        /// <param name="transform">the transform the animation applies to</param>
        /// <param name="target">the component the animation applies to</param>
        /// <returns>value of the animated property. If there is no value at a specific time, should return null (<see cref="TData"/> may need to be nullable in this case)</returns>
        protected TData? getValue(Transform transform, TComponent target);
    }
}