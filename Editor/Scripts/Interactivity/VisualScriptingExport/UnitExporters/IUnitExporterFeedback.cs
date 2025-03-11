using Unity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public interface IUnitExporterFeedback
    {
        UnitLogs GetFeedback(IUnit unit);
    }
}