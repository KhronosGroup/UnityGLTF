namespace Unity.VisualScripting
{
    /// <summary>
    /// Adds the easing preset picker and curve preview to the interpolate units that expose
    /// Point A / Point B control points. <see cref="VariableInterpolateUnitWidget"/> carries the
    /// same addon but needs its own base because of the variable name inspector.
    /// </summary>
    public abstract class BezierInterpolateUnitWidget<TUnit> : UnitWidget<TUnit> where TUnit : Unit
    {
        protected BezierInterpolateUnitWidget(FlowCanvas canvas, TUnit unit) : base(canvas, unit) { }

        protected abstract ValueInput point1Port { get; }

        protected abstract ValueInput point2Port { get; }

        public override bool foregroundRequiresInput => true;

        protected override bool showHeaderAddon => unit.isDefined;

        protected override float GetHeaderAddonWidth() => BezierEasingWidgetAddon.GetWidth();

        protected override float GetHeaderAddonHeight(float width) => BezierEasingWidgetAddon.GetHeight();

        protected override void DrawHeaderAddon()
        {
            BezierEasingWidgetAddon.Draw(headerAddonPosition, point1Port, point2Port);
        }
    }

    [Widget(typeof(InterpolateMember))]
    public sealed class InterpolateMemberWidget : BezierInterpolateUnitWidget<InterpolateMember>
    {
        public InterpolateMemberWidget(FlowCanvas canvas, InterpolateMember unit) : base(canvas, unit) { }

        protected override ValueInput point1Port => unit.pointA;

        protected override ValueInput point2Port => unit.pointB;
    }

    public abstract class MaterialInterpolateUnitWidget<TUnit, TValue> : BezierInterpolateUnitWidget<TUnit>
        where TUnit : AbstractMaterialInterpolate<TValue>
    {
        protected MaterialInterpolateUnitWidget(FlowCanvas canvas, TUnit unit) : base(canvas, unit) { }

        protected override ValueInput point1Port => unit.pointA;

        protected override ValueInput point2Port => unit.pointB;
    }

    [Widget(typeof(MaterialColorInterpolate))]
    public sealed class MaterialColorInterpolateWidget : MaterialInterpolateUnitWidget<MaterialColorInterpolate, UnityEngine.Color>
    {
        public MaterialColorInterpolateWidget(FlowCanvas canvas, MaterialColorInterpolate unit) : base(canvas, unit) { }
    }

    [Widget(typeof(MaterialFloatInterpolate))]
    public sealed class MaterialFloatInterpolateWidget : MaterialInterpolateUnitWidget<MaterialFloatInterpolate, float>
    {
        public MaterialFloatInterpolateWidget(FlowCanvas canvas, MaterialFloatInterpolate unit) : base(canvas, unit) { }
    }

    [Widget(typeof(MaterialOffsetInterpolate))]
    public sealed class MaterialOffsetInterpolateWidget : MaterialInterpolateUnitWidget<MaterialOffsetInterpolate, UnityEngine.Vector2>
    {
        public MaterialOffsetInterpolateWidget(FlowCanvas canvas, MaterialOffsetInterpolate unit) : base(canvas, unit) { }
    }

    [Widget(typeof(MaterialScaleInterpolate))]
    public sealed class MaterialScaleInterpolateWidget : MaterialInterpolateUnitWidget<MaterialScaleInterpolate, UnityEngine.Vector2>
    {
        public MaterialScaleInterpolateWidget(FlowCanvas canvas, MaterialScaleInterpolate unit) : base(canvas, unit) { }
    }
}
