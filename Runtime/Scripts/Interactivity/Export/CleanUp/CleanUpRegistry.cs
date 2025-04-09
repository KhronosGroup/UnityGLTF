using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Export
{
    public static class CleanUpRegistry
    {
   
        private static List<ICleanUp> cleanUpRegistry = new List<ICleanUp>();

        public static void RegisterCleanUp(ICleanUp cleanUp)
        {
            cleanUpRegistry.Add(cleanUp);
        }
        
        public static bool StartCleanUp(InteractivityExportContext context)
        {
            var task = new CleanUpTask(context);
            foreach (var cleanUp in cleanUpRegistry)
            {
                cleanUp.OnCleanUp(task);
            }
            
            return task.HasChanges;
        }
    }
}