#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityGLTF.Timeline
{
    internal interface AnimationTrack
    {
        Object AnimatedObject { get; }
        string PropertyName { get; }
        double[] Times { get; }
        object[] Values { get; }
        
        double? LastTime { get; }
        object? LastValue { get; }
        
        void SampleIfChanged(double time);
    }

    internal abstract class BaseAnimationTrack<TObject, TData> : AnimationTrack where TObject : Object
    {
        private readonly Dictionary<double, TData> samples;
        
        private readonly AnimationData animationData;
        private readonly AnimationSampler<TObject, TData> sampler;
        private Tuple<double, TData>? lastSample = null;
        private Tuple<double, TData>? secondToLastSample = null;

        public Object AnimatedObject => sampler.GetTarget(animationData.transform);
        public string PropertyName => sampler.PropertyName;
        public double[] Times => samples.Keys.ToArray();
        
        public object[] Values => samples.Values.Cast<object>().ToArray();
        internal TData[] values => samples.Values.ToArray();

        public double? LastTime => lastSample?.Item1;
        public object? LastValue => lastValue;
        internal TData? lastValue => lastSample != null ? lastSample.Item2 : default;
        
        protected BaseAnimationTrack(AnimationData tr, AnimationSampler<TObject, TData> plan, double time) {
            this.animationData = tr;
            this.sampler = plan;
            samples = new Dictionary<double, TData>();
            SampleIfChanged(time);
        }

        protected BaseAnimationTrack(AnimationData tr, AnimationSampler<TObject, TData> plan, double time, Func<TData, TData> overrideInitialValueFunc) {
            this.animationData = tr;
            this.sampler = plan;
            samples = new Dictionary<double, TData>();
            recordSampleIfChanged(time, overrideInitialValueFunc(sampler.sample(animationData)));
        }
        
        public void SampleIfChanged(double time) => recordSampleIfChanged(time, sampler.sample(animationData));
        
        protected void recordSampleIfChanged(double time, TData value) {
            if (value == null || (value is Object o && !o)) return;
            // As a memory optimization we want to be able to skip identical samples.
            // But, we cannot always skip samples when they are identical to the previous one - otherwise cases like this break:
            // - First assume an object is positioned at coordinates (1,2,3)
            // - At some point in time, it is "instantaneously" teleported to (4,5,6)
            // If we simply skip identical samples on insert, instead of an almost instantaneous
            // teleport we get a linearly interpolated scale change because only two samples will be recorded:
            // - one at (1,2,3) at the start of time
            // - (4,5,6) at the time of the visibility change
            // What we want to get is
            // - one sample with (1,2,3) at the start,
            // - one with the same value right before the instantaneous teleportation,
            // - and then at the time of the change, we need a sample at (4,5,6)
            // With this setup, now the linear interpolation only has an effect in the
            // very short duration between the last two samples and we get the animation we want.

            // How do we achieve both?
            // Always sample & record and then on adding the next sample(s) we check
            // if the *last two* samples were identical to the current sample.
            // If that is the case we can remove/overwrite the middle sample with the new value.
            if (lastSample != null
                && secondToLastSample != null
                && lastSample!.Item2!.Equals(secondToLastSample.Item2)
                && lastSample!.Item2!.Equals(value)) { samples.Remove(lastSample.Item1); }

            samples[time] = value;
            secondToLastSample = lastSample;
            lastSample = new Tuple<double, TData>(time, value); }
    }

    internal sealed class AnimationTrack<TObject, TData> : BaseAnimationTrack<TObject, TData> where TObject : Object
    {
        public AnimationTrack(AnimationData tr, AnimationSampler<TObject, TData> plan, double time) : base(tr, plan, time) { }
    }
}