using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Unity.VisualScripting
{
    [Descriptor(typeof(InterpolateMember))]
    public class InterpolateMemberDescriptor : MemberUnitDescriptor<InterpolateMember>
    {
        public InterpolateMemberDescriptor(InterpolateMember unit) : base(unit) { }

        protected override ActionDirection direction => ActionDirection.Set;

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
        {
            base.DefinedPort(port, description);
        }
        
        protected override string DefinedTitle()
        {
            return "Interpolate " + base.DefinedTitle();
        }
        
        protected override string DefinedShortTitle()
        {
            return DefinedTitle();
        }
    }
    
    [FuzzyOption(typeof(InterpolateMember))]
    [UsedImplicitly]
    public class InterpolateUnitOptions : MemberUnitOption<InterpolateMember>
    {
        public InterpolateUnitOptions() : base() { }

        public InterpolateUnitOptions(InterpolateMember unit) : base(unit) { }

        protected override ActionDirection direction => ActionDirection.Set;

        protected override bool ShowValueOutputsInFooter()
        {
            return false;
        }

        protected override string Label(bool human)
        {
            return base.Label(human).Replace("Set", "Interpolate");
        }

        protected override string Haystack(bool human)
        {
            return base.Haystack(human).Replace("Set", "Interpolate");
        }
    }
    
    [InitializeAfterPlugins]
    [UsedImplicitly]
    public static class InterpolateMemberAddToFinderExtension
    {
        static InterpolateMemberAddToFinderExtension()
        {
            UnitBase.staticUnitsExtensions.Add(GetStaticOptions);
        }

        private static IEnumerable<IUnitOption> GetStaticOptions()
        {
            CodebaseSubset codebase = typeof(UnitBase).GetField("codebase", (BindingFlags)(-1)).GetValue(null) as CodebaseSubset;
            
            foreach (var member in codebase.members)
            {
                foreach (var memberOption in GetMemberOptions(member))
                {
                    yield return memberOption;
                }
            }
        }
        
        private static IEnumerable<IUnitOption> GetMemberOptions(Member member)
        {
            // Operators are handled with special math units
            // that are more elegant than the raw methods
            if (member.isOperator)
            {
                yield break;
            }

            // Conversions are handled automatically by connections
            if (member.isConversion)
            {
                yield break;
            }
            
            // Interpolation is only available for these types:
            // int, float, Vector2, Vector3, Vector4, Quaternion, Color, Matrix4x4
            
            if (member.type != typeof(int) &&
                member.type != typeof(float) &&
                member.type != typeof(UnityEngine.Vector2) &&
                member.type != typeof(UnityEngine.Vector3) &&
                member.type != typeof(UnityEngine.Vector4) &&
                member.type != typeof(UnityEngine.Quaternion) &&
                member.type != typeof(UnityEngine.Color) &&
                member.type != typeof(UnityEngine.Matrix4x4))
            {
                yield break;
            }
            
            // If the declaring type is Vector2, Vector3, Vector4, Quaternion, Color, or Matrix4x4, we can't interpolate
            // e.g. we can't interpolate Position.x, we can only interpolate the whole Position
            if (member.declaringType == typeof(UnityEngine.Vector2) ||
                member.declaringType == typeof(UnityEngine.Vector3) ||
                member.declaringType == typeof(UnityEngine.Vector4) ||
                member.declaringType == typeof(UnityEngine.Quaternion) ||
                member.declaringType == typeof(UnityEngine.Color) ||
                member.declaringType == typeof(UnityEngine.Matrix4x4) ||
                // these are basically wrappers around the above
                member.declaringType == typeof(UnityEngine.Bounds) ||
                member.declaringType == typeof(UnityEngine.BoundsInt) ||
                member.declaringType == typeof(UnityEngine.Rect))
            {
                yield break;
            }
            
            // Transform is a special case; we only want to allow localPosition, localRotation, and localScale
            if ((member.declaringType == typeof(UnityEngine.Transform) || member.declaringType == typeof(UnityEngine.RectTransform)) &&
                member.name != "localPosition" &&
                member.name != "localRotation" &&
                member.name != "localScale")
            {
                yield break;
            }

            if (member.isAccessor)
            {
                if (member.isPubliclySettable)
                {
                    yield return new InterpolateMember(member).Option();
                }
            }
        }
    }
}