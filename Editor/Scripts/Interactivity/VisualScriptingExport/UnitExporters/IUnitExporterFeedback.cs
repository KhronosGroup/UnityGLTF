using Unity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public interface IUnitExporterFeedback
    {
        UnitLogs GetFeedback(IUnit unit);
    }
}