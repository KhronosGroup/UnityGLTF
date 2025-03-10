using Unity.VisualScripting;
using UnityGLTF.Interactivity;

namespace UnityGLTF.Interactivity.Export
{
    public interface IUnitExporterFeedback
    {
        UnitLogs GetFeedback(IUnit unit);
    }
}