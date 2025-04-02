namespace Unity.VisualScripting
{
    [Descriptor(typeof(VariableInterpolate))]
    public class VariableInterpolateUnitDescriptor<TVariableUnit> : UnitDescriptor<TVariableUnit> where TVariableUnit : VariableInterpolate
    {
        public VariableInterpolateUnitDescriptor(TVariableUnit unit) : base(unit) { }

        protected override EditorTexture DefinedIcon()
        {
            return BoltCore.Icons.VariableKind(unit.kind);
        }
    }
}
