using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal interface AnimationTrack
    {
        Object animatedObject { get; }
        string propertyName { get; }
        double[] times { get; }
        object[] values { get; }
        void SampleIfChanged(double time);
    }

    internal abstract class BaseAnimationTrack<TObject, TData> : AnimationTrack where TObject : Object
    {
        private readonly Dictionary<double, TData> samples;
        
        
        private AnimationData animationData;
        private AnimationSampler<TObject, TData> sampler;
        private Tuple<double, TData> lastSample = null;
        private Tuple<double, TData> secondToLastSample = null;

        public Object animatedObject => sampler.GetTarget(animationData.transform);
        public abstract string propertyName { get; }
        public double[] times => samples.Keys.ToArray();
        
        public object[] values => samples.Values.Cast<object>().ToArray();
        
        protected BaseAnimationTrack(AnimationData tr, AnimationSampler<TObject, TData> plan, double time) {
            this.animationData = tr;
            this.sampler = plan;
            samples = new Dictionary<double, TData>();
            SampleIfChanged(time);
        }
        
        public void SampleIfChanged(double time) { 
            var value = sampler.sample(animationData);
            if (value == null || (value is Object o && !o)) return;
            // As a memory optimization we want to be able to skip identical samples.
            // But, we cannot always skip samples when they are identical to the previous one - otherwise cases like this break:
            // - First assume an object is invisible at first (by having a scale of (0,0,0))
            // - At some point in time, it is instantaneously set "visible" by updating its scale from (0,0,0) to (1,1,1)
            // If we simply skip identical samples on insert, instead of a instantaneous
            // visibility/scale changes we get a linearly interpolated scale change because only two samples will be recorded:
            // - one (0,0,0) at the start of time
            // - (1,1,1) at the time of the visibility change
            // What we want to get is
            // - one sample with (0,0,0) at the start,
            // - one with the same value right before the instantaneous change,
            // - and then at the time of the change, we need a sample with (1,1,1)
            // With this setup, now the linear interpolation only has an effect in the very short duration between the last two samples and we get the animation we want.

            // How do we achieve both?
            // Always sample & record and then on adding the next sample(s) we check
            // if the *last two* samples were identical to the current sample.
            // If that is the case we can remove/overwrite the middle sample with the new value.
            if (lastSample != null
                && secondToLastSample != null
                && lastSample.Item2.Equals(secondToLastSample.Item2)
                && lastSample.Item2.Equals(value)) { samples.Remove(lastSample.Item1); }

            samples[time] = value;
            secondToLastSample = lastSample;
            lastSample = new Tuple<double, TData>(time, value); }
    }

    internal class AnimationTrack<TObject, TData> : BaseAnimationTrack<TObject, TData> where TObject : Object
    {
        public AnimationTrack(AnimationData tr, AnimationSampler<TObject, TData> plan, double time) : base(tr, plan, time) { }
        public override string propertyName => "temp";
    }
    
    // internal class VisibilityTrack : AnimationData
    // {
    //     
    // }
}